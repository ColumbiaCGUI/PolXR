"""
Mat_to_Mesh.py
This script converts CReSIS formatted .MAT radar files to OBJ/MTL/PNG Mesh files

@authors: jksalzman, sicordero, gyou, spark
"""
import os
import pandas as pd
import geopandas as gpd
import numpy as np
import matplotlib.pyplot as plt
import h5py
from scipy.io import loadmat
#import impdar   #a secret tool we will use later to crop radargrams
from pipeline.center_mesh import center_obj
from scipy.io import loadmat
from scipy.io.matlab import matfile_version

# Constants DO NOT TOUCH
cAir = 299792458  # m/s
cIce = 1.68e8	  # m/s

def cresis_to_mesh(
    matfile,
    outdir,
    crs_old,
    crs_new,
    set_origin=False,
    clip_noise=False
):
    def check_mat_file_version(file_path):
        try:
            # Attempt to open with h5py (post-7.3 HDF5 format)
            with h5py.File(file_path, 'r') as f:
                print(f"{file_path} is a MATLAB v7.3+ (HDF5) file.")
                return "post"
        except (OSError, IOError):
            # If h5py fails, check for pre-7.3 format
            try:
                with open(file_path, 'rb') as f:
                    major_version, minor_version = matfile_version(f)
                    print(f"{file_path} is a MATLAB pre-v7.3 file (v{major_version}.{minor_version}).")
                    return "pre"
            except Exception as e:
                print(f"Unable to determine the version for {file_path}. Error: {e}")
                return "Unknown"
    
    # Check version and Read MatFile
    version = check_mat_file_version(matfile)
    
    if(version=='post'):    # In case of a Post-7.3 file version
        mat = h5py.File(matfile, 'r+')
        data = None
        singletons = dict()
        horizontal = dict()
        vertical = dict()
        for k, v in mat.items():
            # Check if v is a h5py dataset and convert to np.ndarray
            if isinstance(v, h5py.Dataset):
                v = v[()] 
            
            if isinstance(v, np.ndarray):
                if v.shape == (1, 1):
                    singletons[k] = v
                elif k.lower() == 'data':
                    data = pd.DataFrame(v)
                elif k.lower() in {'time', 'depth'}:
                    vertical[k.capitalize()] = v.flatten()
                elif k.lower() in {'longitude','latitude', 'surface', 'elevation', 'bottom', 'heading', 'pitch', 'roll', 'gps_time'}:
                    horizontal[k.capitalize()] = v.flatten()
                else:
                    pass
 
    else:
        # If the version is a Pre-7.3 MatFile 
        mat = loadmat(matfile)
        data = None
        singletons = dict()
        horizontal = dict()
        vertical = dict()
        #print(mat.keys())
        for k, v in mat.items():
            if isinstance(v, np.ndarray):
                # Search by key
                if v.shape == (1, 1):
                    singletons[k] = v
                elif k.lower() == 'data':
                    data = pd.DataFrame(v).T
                elif k.lower() in {'time', 'depth'}:
                    vertical[k] = v.flatten()
                elif k.lower() in {'longitude','latitude', 'surface', 'elevation'}:
                    horizontal[k] = v.flatten()
                else:
                    horizontal[k] = v.flatten()
                    
                    
    # Convert coordinates to points
    horizontal['geometry'] = gpd.GeoSeries.from_xy(
        horizontal['Longitude'],
        horizontal['Latitude'],
        crs=crs_old)

    # Create GeoDataFrames so that everything is in Pandas
    hdf = gpd.GeoDataFrame(horizontal, crs=crs_old, geometry='geometry')
    vdf = pd.DataFrame(vertical)

    # Calculate z values
    surface = np.nan_to_num(
        hdf['Surface'],
        copy=False,
        nan=np.nanmean(hdf['Surface']))
    time = np.tile(vdf['Time'], (surface.shape[0], 1))
    top = np.array(hdf['Elevation'] - (0.5 * cAir * surface)).reshape(-1, 1)
    z = top - 0.5*(cIce * (time - surface.reshape(-1, 1)))
    

    # Project coordinates to new coordinate system
    if crs_new != crs_old:
        hdf = hdf.to_crs(crs_new)
    x = np.tile(hdf.geometry.x.to_numpy().reshape(-1, 1), (1, z.shape[1]))
    y = np.tile(hdf.geometry.y.to_numpy().reshape(-1, 1), (1, z.shape[1]))
    
    arr = np.empty((*x.shape, 4))
    arr[:, :, 0] = x
    arr[:, :, 1] = y
    arr[:, :, 2] = z
    arr[:, :, 3] = data
    
    # Clip empty space  TO-DO
    if clip_noise:
        k = 5 # after this many empty rows, assume we're just seeing noise
        mask = []
        empty_rows = 0
        remove_rows = False
        for row in arr:
            if np.all(row == 0): # we could change this to look for max or mean
                empty_rows += 1
                if empty_rows == k:
                    remove_rows = True 
            else:
                empty_rows = 0
            mask.append(not remove_rows)
        arr = arr[mask, :, :]

    # create mesh
    return create_mesh(matfile, outdir, arr, set_origin)

def create_mesh(matfile, outdir, arr, set_origin=False):
    """
    Creates a triangle mesh out of the data.

    Parameters
    ----------
    matfile : str
        Filepath of the mat file.
    outdir : str
        Directory for the output files to be put into.
    arr : np.array
        The array computed in the previous function.
    set_origin : bool, optional
        Whether to reset the coordinates so they intersect with the origin. 
        The default is False.
        This should only be set to True while debugging.

    Returns
    -------
    out_obj : str
        The filepath of the outputted obj file.

    """

    # filenames
    basename = os.path.basename(matfile).replace('.mat', '')
    basepath = os.path.join(outdir, basename)
    out_png = f'{basepath}.png'
    out_mtl = f'{basepath}.mtl'
    out_obj = f'{basepath}.obj'
    #reference of mtl file from obj file
    mtl_ref_obj = f'{os.path.relpath(out_mtl, start=os.path.dirname(out_obj))}'
    #reference of png file from mtl file
    png_ref_mtl = f'{os.path.relpath(out_png, start=os.path.dirname(out_mtl))}'
    out_line = f'{basepath.replace(basename, f"FlightLine_{basename}")}.obj'

    # reset origin maybe
    if set_origin:
        arr[:, :, 0] -= arr[:, :, 0].min()
        arr[:, :, 1] -= arr[:, :, 1].min()

    # write png file
        # every single pixel gets converted to a full resoltion img 
    cvals = 20 * np.log10(np.abs(arr[:, :, 3]))
    c = np.abs((cvals - np.min(cvals)) / (np.max(cvals) - np.min(cvals))) * 256
    plt.imsave(out_png, np.flipud(c.T), cmap='gray')

    # write mtl file
    with open(out_mtl, 'w') as mtl:
        mtl.write(f'newmtl {basename}\n')
        mtl.write('Ka  0.0000  0.0000  0.0000\n')
        mtl.write('Kd  1.0000  1.0000  1.0000\n')
        mtl.write('illum 1\n')
        mtl.write(f'map_Kd {png_ref_mtl}')

    # write obj file
    polyline = list()
    with open(out_obj, 'w') as obj:
        
        # include material
        obj.write(f'mtllib {mtl_ref_obj}\n')
        
        # iterate through horizontal coordinates
        for i in range(arr.shape[0]-1):
            
            # extract coordinate triples for the corners of the next rectangle
            ul = ' '.join(arr[i, 0, :3].astype(str))        # upper left
            ll = ' '.join(arr[i, -1, :3].astype(str))       # lower left
            lr = ' '.join(arr[i+1, -1, :3].astype(str))     # lower right
            ur = ' '.join(arr[i+1, 0, :3].astype(str))      # upper right
            
            # add vertices
            for vertex in (ul, ll, lr, ur):
                obj.write(f'v {vertex}\n')
            
            # add uv mapping coordinates
            obj.write(f'vt {i/(arr.shape[0]-1)} 0.0\n')       # upper left
            obj.write(f'vt {i/(arr.shape[0]-1)} 1.0\n')       # lower left
            obj.write(f'vt {(i+1)/(arr.shape[0]-1)} 1.0\n')   # lower right
            obj.write(f'vt {(i+1)/(arr.shape[0]-1)} 0.0\n')   # upper right
            
            # store points for polyline
            if not i:
                polyline.append(ul)
            polyline.append(ur)
        
        # create faces
        obj.write('g mesh\n')
        for i in range(arr.shape[0]-1):
            
            # create two triangles
            for idxs in [(0, 1, 2), (0, 2, 3)]:
                face_string = ' '.join([f'{i*4+j+1}/{i*4+j+1}' for j in idxs])
                obj.write(f'f {face_string}\n')
        
        obj.write('g\n')
        obj.write(f'usemtl {basename}\n\n')
        
    # write polyline file
    # TODO: should this be done elsewhere since we have one polyline per flight
    with open(out_line, 'w') as obj2:
        
        for pt in polyline:
            obj2.write(f'v {pt}\n')
        obj2.write(f'l {" ".join([str(i+1) for i in range(len(polyline))])}')

    return out_obj


def process_flightline(flightline_id, crs_old, crs_new):
    """
    Processes all the MAT files related to the specified flightline_id as separate flightline segments.

    Parameters
    ----------
    flightline_id : str
        The flightline identifier (e.g., 'FL123').
    crs_old : int, optional
        The coordinate reference system of the input data (default is WGS84, EPSG:4326).
    crs_new : int, optional
        The desired coordinate reference system for output (default is NSIDC Sea Ice Polar Stereographic North, EPSG:3413).
    """

    input_dir = f'pipeline/cresis_data/{flightline_id}/mat'

    
    # Process all MAT files in the directory
    for matfile in os.listdir(input_dir):
        if matfile.endswith('.mat'):
            matfile_path = os.path.join(input_dir, matfile)
            
            segment_id = matfile.replace(f'Data_{flightline_id}_', '').replace('.mat', '')
            
            outdir = os.path.join('../PolXR/Assets/AppData/Flightlines', flightline_id, segment_id)
            
            if not os.path.exists(outdir):
                os.makedirs(outdir)
            
            # Call cresis_to_mesh to process the MAT file and create mesh
            cresis_to_mesh(matfile_path, outdir, crs_old, crs_new)
            
            print(f'Successfully saved flightline segment {segment_id} to {outdir}')