import argparse

def run_radar(kml_url, crs_old=4326, crs_new=3413):
    """
    Handle radar data processing by invoking the `fetch_cresis_data` logic.
    """
    print(f"Running radar data processing for KML URL: {kml_url}")
    from pipeline.fetch_cresis_data import download_flight_data
    from pipeline.mat_to_mesh import process_flightline
    flightline_id = download_flight_data(kml_url)
    print(f"\nProcessing flightline: {flightline_id}")

    process_flightline(flightline_id, crs_old, crs_new)

def run_dem(dem_dir_name):
    """
    Handle DEM processing by invoking the `stage_dems` logic.
    """
    print(f"Running DEM processing for directory: {dem_dir_name}")
    from pipeline.dem_to_mesh import stage_dems
    stage_dems(dem_dir_name)

def main():
    """
    Main entry point for the script.
    """
    parser = argparse.ArgumentParser(
        description="Process radar data or DEM data."
    )
    subparsers = parser.add_subparsers(
        title="commands", description="Available commands", dest="command"
    )

    # Subcommand for radar
    radar_parser = subparsers.add_parser(
        "radar", help="Process radar data from a KML URL."
    )
    radar_parser.add_argument(
        "kml_url", type=str, help="URL to the radar KML file."
    )
    # Optional crs_new parameter with default value of 3413
    radar_parser.add_argument(
        "crs_new", type=int, nargs="?", default=3413, help="Optional new CRS (default: 3413)."
    )

    # Subcommand for dem
    dem_parser = subparsers.add_parser(
        "dem", help="Process DEM data from a directory name."
    )
    dem_parser.add_argument(
        "dem_dir_name", type=str, help="Name of the DEM directory to process."
    )

    # Optional depth argument with default value of 9
    dem_parser.add_argument(
        "depth", type=int, nargs="?", default=9, help="Depth value for DEM processing (default: 9)."
    )

    # Parse arguments
    args = parser.parse_args()

    # Handle commands
    if args.command == "radar":
        run_radar(args.kml_url, crs_old=4326, crs_new=args.crs_new)
    elif args.command == "dem":
        run_dem(args.dem_dir_name)
    else:
        parser.print_help()

if __name__ == "__main__":
    main()