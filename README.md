# SSVEP Game

This repository contains the SSVEP Game & subsequent movement video generation

## Directory structure

```
SSVEP-Game
├── Data/                  *****suggested file structure for using the processing notebooks
│   ├── P001/
│   │   └── movements_P001_BW_M1.csv
│   │   └── movements_P001_C3S1_M2.csv
│   ├── P002....
├── Movement-Videos/                ← folde than contains the functions and notebooks for generating movement videos
│   ├── Functions/                  ← custom python modules used by movement video notebooks
|   │   └── file_info.py            ← helper functions for getting file info from file name
|   │   └── map_templates.py        ← class for structure of the game maps
│   |   └── vis_video.py            ← helper functions generation of movement videos
│   |   └── visualize_map.py        ← helper functions visualizing the maps as a grid image
|   ├── Notebooks/                  ← folder that contains the jupyter notebooks for the generation of movement videos and summaries
|   │   └── movement_counter.ipynb  ← notebook to produce a summary of all participant movements
|   │   └── movement_videos.ipynb   ← notebook to produce movement videos, one file at a time
├── SSVEP-Game-Unity/               ← folder that contains the unity project used to present the SSVEP Game
│   └── ... 
└── README.md                   ← this file
```

## Data
- **`Data/P00X`** – movement logs `movements_P00X_XXXX_MX.csv` created during the playing of the 
                    SSVEP Game Unity project. Movement videos (with the same file name but a `.mp4`
                    extension) will be exported here.

## Code
- **`Functions/file_info.py`** – helper functions to extract file information componenets from file name.
- **`Functions/map_templates.py`** - defines the Map class and recreates the map layouts used in the SSVEP
                                    Game as coordinates.
- **`Functions/vis_video.py`** - helper functions used to generate the movement videos from the movement
                                log `.csv` files.
- **`Functions/visualize_map.py`** - helper functions to visualize the map coordinates as a grid for the 
                                    movement videos.

## Notebooks
1. **`movement_counter.ipynb`** – this notebook imports all movement log `.csv` files and creates a summary
                                of the number of movements (failed and total) taken during the Standard and 
                                Personal Game playthroughs for each participant. Exports as 
                                `participant_movements_summary.csv`.

2. **`movement_videos.ipynb`** – this notebook takes the import of a single `.csv` file and generates the
                                movement video for that file and saves the `.mp4` file in the same Data
                                folder.

## Usage
1. Install the Python dependencies listed in the notebooks (pandas, numpy,
    matplotlib, etc.).
    * Use dependencies.yml
2. Set up data in the suggested structure
3. Open `movement_counter.ipynb` in VS Code and execute all cells to generate the
   `participant_movements_summary.csv` file.
4. Open `movement_videos.ipynb` in VS Code and execute all cells to generate the
   `.mp4` file for each game playthrough.  
---
