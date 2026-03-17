import numpy as np
import matplotlib.pyplot as plt
from matplotlib.colors import ListedColormap

# At the top of your file, add this after imports
def create_custom_colormap():
    """Create a colormap for: 0 = paths, 1 = obstacles/empty, 2 = gems, 3 = start position."""
    colors = [(46/225, 37/255, 133/255), (221/255, 221/255, 221/255),  (93/255, 168/255, 153/255), (159/255, 74/255, 150/255)]
    return ListedColormap(colors)

def visualize_map(MapClass):
    width = MapClass.width
    height = MapClass.height

    grid = np.zeros((height, width))

    # Obstacles = 1
    for x, y in MapClass.obstacles:
        grid[y-1, x-1] = 1

    # Gems = 2
    for x, y in MapClass.gems:
        grid[y-1, x-1] = 2

    # Start = 3
    sx, sy = MapClass.start_pos
    grid[sy-1, sx-1] = 3

    fig, ax = plt.subplots(figsize=(6, 8))

    # Align tiles exactly to coordinate grid
    ax.imshow(grid, origin='lower', extent=[0, width, 0, height])

    # Set ticks at cell edges (0..width)
    ax.set_xticks(np.arange(0, width + 1, 1))
    ax.set_yticks(np.arange(0, height + 1, 1))

    # Draw gridlines at cell boundaries
    ax.set_xticks(np.arange(0, width + 1, 1), minor=False)
    ax.set_yticks(np.arange(0, height + 1, 1), minor=False)

    ax.grid(which='major', color='black', linewidth=1)

    # Invert Y axis
    ax.invert_yaxis()
    ax.set_title(f"Map Visualization: {MapClass.__name__}")
    plt.show()

def vis_map_base(MapClass):
    width = MapClass.width
    height = MapClass.height

    grid = np.zeros((height, width))

    # Obstacles = 1
    for x, y in MapClass.obstacles:
        grid[y-1, x-1] = 1

    # Gems = 2
    for x, y in MapClass.gems:
        grid[y-1, x-1] = 2

    # Start = 3
    sx, sy = MapClass.start_pos
    grid[sy-1, sx-1] = 3

    fig, ax = plt.subplots(figsize=(6, 8))

    # Align tiles exactly to coordinate grid
    custom_cmap = create_custom_colormap()
    ax.imshow(grid, origin='lower', extent=[0, width, 0, height], cmap=custom_cmap, vmin=0, vmax=3)

    # Set ticks at cell edges 
    ax.set_xticks(np.arange(0, width + 1, 1))
    ax.set_yticks(np.arange(0, height + 1, 1))

    # Plot grid lines (linewidth > 1 so there is no overlap of colored squares visible beneath grid lines)
    ax.grid(which='major', color='black', linewidth=1.15)

    # Invert Y axis
    ax.invert_yaxis()

    return fig, ax
 
def visualize_map_pos(MapClass, old_position, new_position):
    """ 
    Visualize the map with a single marker position or a list of markers
    """
    # Draw base map
    fig, ax = vis_map_base(MapClass)

    # Handle single tuple or list of tuples
    if isinstance(old_position[0], (int)):
        # Single coordinate tuple like (7, 8)
        ox, oy = old_position
        nx, ny = new_position
        ax.plot(ox-0.8, oy-0.5, 'bo')  # Old position in blue
        ax.plot(nx-0.2, ny-0.5, 'ro')  # New position in red
        ax.arrow(ox-0.5, oy-0.5, nx-ox, ny-oy, head_width=0.2, head_length=0.2, fc='green', ec='green')
    else:
        # List of tuples
        for (ox, oy), (nx, ny), in zip(old_position, new_position): 
            ax.plot(ox-0.5, oy-0.5, 'bo')  # Old position in blue
            ax.plot(nx-0.5, ny-0.5, 'ro')  # New position in red
            ax.arrow(ox-0.5, oy-0.5, nx-ox, ny-oy, head_width=0.2, head_length=0.2, fc='green', ec='green')

    ax.set_title(f"Map Visualization with Movements: {MapClass.__name__}")
    plt.show()