"""
DEM to Mesh Conversion Script
"""

import os, json
import pandas as pd
import open3d as o3d
import numpy as np
import rasterio
import trimesh
from pipeline.center_mesh import center_obj

def dem_to_mesh(dem_name: str, filename: str):
    """
    Converts a DEM file in GeoTIF format into a mesh and outputs it as an OBJ file.

    Parameters:
    - dem_name (str): Name of the DEM dataset (e.g., 'Petermann').
    - filename (str): Name of the file to process (e.g., 'surface.tif').

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
    elevation = tif.read(1)

    # Get affine transformation to convert from pixel coordinates to geospatial coordinates
    transform = tif.transform
    
    # Generate mesh vertices
    rows, cols = elevation.shape
    vertices = []

    for row in range(rows):
        for col in range(cols):
            # Transform pixel coordinates to geospatial coordinates (x, y)
            x, y = transform * (col, row)
            z = elevation[row, col]
            vertices.append([x, y, z])

    vertices = np.array(vertices)
    
    # Generate mesh faces (each pixel is a quad, which we will convert to two triangles)
    faces = []

    for row in range(rows - 1):
        for col in range(cols - 1):
            # Get vertex indices for the current quad
            v0 = row * cols + col
            v1 = v0 + 1
            v2 = (row + 1) * cols + col
            v3 = v2 + 1
            
            # Split quad into two triangles
            faces.append([v0, v1, v2])  # First triangle
            faces.append([v1, v3, v2])  # Second triangle

    faces = np.array(faces)

    # Create the mesh using trimesh
    mesh = trimesh.Trimesh(vertices=vertices, faces=faces)

    # Save mesh as OBJ
    #o3d.io.write_triangle_mesh(output_path, mesh)
    mesh.export(output_path)

    # Calculate centroid
    vertices = np.asarray(mesh.vertices)
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