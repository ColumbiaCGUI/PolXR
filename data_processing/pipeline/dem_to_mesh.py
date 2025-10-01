"""
DEM to Mesh Conversion Script
"""

import os, json
import pandas as pd
import open3d as o3d
import numpy as np
import rasterio
from scipy.ndimage import zoom

from pipeline.center_mesh import center_obj

def dem_to_mesh(dem_name: str, filename: str):
    """
    Converts a DEM file in GeoTIF format into a mesh and outputs it as an OBJ file.

    Parameters:
    - dem_name (str): Name of the DEM dataset (e.g., 'Petermann').
    - filename (str): Name of the file to process (e.g., 'surface.tif').
    - [TODO] texture_image (str): Name of surface texture image file (e.g. 'velocity.tif', 'sentinel.png')
    - downsample (int): Downsampling factor default 0.8 (80%) 

    Output:
    The mesh is saved to PolXR/Assets/AppData/DEMs/{dem_name}/{filename.replace('.tif', '.obj')}.
    """
    # Construct file paths
    input_path = f"pipeline/dems/{dem_name}/{filename}"
    output_path = f"../PolXR/Assets/AppData/DEMs/{dem_name}/{filename.replace('.tif', '.obj')}"
    
    # Ensure output directory exists
    os.makedirs(os.path.dirname(output_path), exist_ok=True)
       
    # Load GeoTIF
    tif = rasterio.open(input_path)
    
    # Read the elevation data (band 1)
    elevation = tif.read(1, masked=True).filled(np.nan)

    # Get affine transformation to convert from pixel coordinates to geospatial coordinates
    transform = tif.transform
    crs = tif.crs
    
    # Begin downsampling
    downsample = 0.8  # Use 1.0 for no downsampling
    data_ds = zoom(elevation, downsample, order=3)  # cubic
    rows, cols = data_ds.shape
    transform = rasterio.Affine(
        transform.a / downsample,
        transform.b,
        transform.c,
        transform.d,
        transform.e / downsample,
        transform.f,
    )

    # Generate mesh vertices (xyz)
    row_idx, col_idx = np.meshgrid(np.arange(rows), np.arange(cols), indexing="ij")
    xs, ys = rasterio.transform.xy(transform, row_idx, col_idx)
    X, Y = np.array(xs), np.array(ys)

    vertices = np.column_stack((X.ravel(), Y.ravel(), data_ds.ravel()))

    # Generate UV Coordinates
    u = col_idx / (cols - 1)
    v = 1 - (row_idx / (rows - 1))
    uvs = np.column_stack((u.ravel(), v.ravel()))
    
    # Generate mesh faces (2 triangles per quad), couter clockwise for Unity lol
    faces = []
    for i in range(rows - 1):
        for j in range(cols - 1):
            v1 = i * cols + j
            v2 = v1 + 1
            v3 = v1 + cols
            v4 = v3 + 1
            faces.append((v1, v3, v2)) 
            faces.append((v2, v3, v4))
    faces = np.array(faces)


    # Calculate Normals
    normals = np.zeros(vertices.shape, dtype=np.float32)
    for face in faces:
        v1, v2, v3 = vertices[face]
        normal = np.cross(v2 - v1, v3 - v1)
        normal /= np.linalg.norm(normal) + 1e-9
        for idx in face:
            normals[idx] += normal

    # Normalize all normals
    norms = np.linalg.norm(normals, axis=1)
    normals /= norms[:, np.newaxis] + 1e-9


    # Create the mesh variable
    mesh = {
    "vertices": vertices,
    "faces": faces,
    "uvs": uvs,
    "normals": normals
    }

    # Generate OBJ in Unity compatible format
    with open(output_path, "w") as f:
        f.write("# Unity-compatible OBJ generated from GeoTIFF\n")
        # if output_mtl:
        #     f.write(f"mtllib {output_mtl}\n")
        # vertices
        for v in vertices:
            f.write(f"v {v[0]} {v[1]} {v[2]}\n")
        # uvs
        for uv in uvs:
            f.write(f"vt {uv[0]} {uv[1]}\n")
        # normals
        for vn in normals:
            f.write(f"vn {vn[0]} {vn[1]} {vn[2]}\n")
        # faces (Unity wants f v/vt/vn)
        for face in faces:
            f.write("f")
            for idx in face:
                i = idx + 1  # OBJ is 1-based
                f.write(f" {i}/{i}/{i}")
            f.write("\n")

    print(f"OBJ saved: {output_path}")

    """ if "surface" in input_path.lower():
        - TODO texture surface with imagery png with conditional clause OR separate script entirely
        """

    


    # Calculate centroid
    vertices = np.asarray(vertices)
    centroid = {
        "x": np.mean(vertices[:, 0]),
        "y": np.mean(vertices[:, 1]),
        "z": np.mean(vertices[:, 2])
    }

    dem_meta = {
        "centroid": centroid
    }
    dem_meta_path = os.path.join(f"../PolXR/Assets/AppData/DEMs/{dem_name}", 'meta.json')
    with open(dem_meta_path, 'w') as f:
        json.dump(dem_meta, f, indent=4)

    # center_obj(output_path)
    print(f"Mesh saved to {output_path}")
    
    


def stage_dems(dem_name: str):
    """
    Processes and stages both surface and bedrock DEMs for a given DEM name.

    Parameters:
    - dem_name (str): Name of the DEM dataset (e.g., 'Petermann').

    Calls:
    - dem_to_mesh with 'surface.tif'
    - dem_to_mesh with 'bedrock.tif'
    """
    dem_to_mesh(dem_name, "bedrock.tif")
    dem_to_mesh(dem_name, "surface.tif")