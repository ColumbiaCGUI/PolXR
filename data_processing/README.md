# Data Processing

## Overview

This directory contains scripts and data for processing DEM and CReSIS data. The main entry point for running the data processing pipeline is `run.py`.

## Data Directories

- `dem`: Directory containing DEM (Digital Elevation Model) data files.
- `cresis_data`: Directory containing CReSIS (Center for Remote Sensing of Ice Sheets) data files.

## Functional Scripts

- `dem_loader.py`: Script for loading and processing DEM data.
- `cresis_loader.py`: Script for loading and processing CReSIS data.
- `data_cleaner.py`: Script for cleaning and preprocessing data.
- `data_analyzer.py`: Script for analyzing processed data.

## Main Entry Point

- `run.py`: Main script to run the data processing pipeline.

## Installation

### Prerequisites

- Python 3.10 or higher preferred
- Virtual environment (venv)

### Setup

1. Create and activate a virtual environment:

```bash
python -m venv venv
source venv/bin/activate  # On Windows use `venv\Scripts\activate`
```

2. Install the required packages:

```bash
pip install -r requirements.txt
```

3. Run the data processing pipeline:

```bash
python run.py
```

## Usage
Note: Make sure you have the pipeline/cresis_data and pipeline/dems directories. If not, create them locally.

### Pre-processing Radargram Data

The following command fetches radargram .mat data from CReSIS using the `kml_url` parameter and runs the conversion function on every .mat file retrieved. Then, it loads the converted data into the correct Unity directory.

```bash
python run.py radar <kml_url> [crs_new]
```

- `kml_url` (required): The URL to the radar KML file to process.
- `crs_new` (optional): The new coordinate reference system (CRS) for the radar data. Default is 3413.

### Example Usage

```bash
python run.py radar https://data.cresis.ku.edu/data/rds/2008_Greenland_TO/kml_good/Browse_Data_20080802_03.kml
```

### Pre-processing DEM Data

The following command detects the `dem_dir_name` (i.e. Petermann) to convert the .TIF files within that directory into .obj files and automatically load them into the correct Unity directory. Note that the .TIF files are generated through QGIS. Refer to the build instructions [here](https://docs.google.com/document/d/1vaTSFGdMRg9INGP5Ipzsgx5K-AoO1MsLKbJEeksdyGM/edit?tab=t.0#heading=h.ixnj4rkskt8r) to see how to use QGIS. Make sure that when you "Clip Raster By Extent", you save the `surface.tif` and `bedrock.tif` files to a new directory within pipeline/dems. The name for this new directory is what the package detects as dem_dir_name.


```bash
python run.py dem <dem_dir_name>
```

- `dem_dir_name` (required): The name of the directory containing DEM data.

### Example Usage

```bash
python run.py dem Petermann
```

## Additional Documentation
For more detailed information, refer to the [Google Document](https://docs.google.com/document/d/14bAPdM2SI9v9M8cAHKjS-G69ToPziXnMD2ehhc8Psec).
