import matplotlib.pyplot as plt
import cv2
import os
import numpy as np
from visualize_map import vis_map_base

def visualize_map_video(ID, Stim, MapClass, old_positions, new_positions, intended_directions, keypresses, save_video=True, video_filename="movement_animation.mp4"):
    """
    Visualize movements through the map as an animation.

    Parameters:
        * ID : string
            - Participant ID for labeling video
        * Stim : string
            - Stimulus name for labeling video
        * MapClass : object
            - The map class containing map details.
                - Attributes include map width, map height, map obstacles, location of gems, and map start_pos.
        * old_positions : list of (x, y) tuples
            - Previous positions of player on the map.
        * new_positions : list of (x, y) tuples
            - New positions of player after movement.
        * intended_directions : list of (dx, dy) tuples or None or empty tuple
            - Intended movement directions for each movement by the player.
            - None or empty tuple values indicate missing/blank data.
                - Participants 1-9 have this missing data due to refactoring of how movement data was saved during the data collection period.
        * keypresses : list of bool
            - Bool that indicates if the movement was controlled by keypress rather than player.
        * save_video : bool, optional
            - Whether to save the output as a video file.
            - Default is True.
        * video_filename : str, optional
            - The filename for the output video.
            - Default is "movement_animation.mp4".

    Returns:
        None
            - Saves the animation as a video if save_video is True.
    """
    # =====================================================
    # Function Setup
    # =====================================================
    print("Creating movement animation...")

    # Create temporary directory for all frames for this video & an empty list to store frame file paths
    temp_dir = "temp_frames"
    os.makedirs(temp_dir, exist_ok=True)
    frame_files = []

    # Record total steps
    total_steps = len(old_positions)

    # -------- Calculate fail streaks  --------
    # direction_fail_streaks counts consecutive fails in the same intended direction from the same square (for movement files with no missing data)
    # position_fail_streaks counts consecutive fails from the same square regardless of direction (for movement files with missing intended directions)
    direction_fail_streaks, position_fail_streaks = calculate_fail_streaks(old_positions, new_positions, intended_directions)

    # -------- Draw initial frame --------
    f_name = create_initial_frame(MapClass, old_positions, ID, Stim,  total_steps, temp_dir)
    frame_files.append(f_name)

    # =====================================================
    # Process each movement
    # =====================================================
    # Iterate through each movement step in the provided position lists
    # Unpack x and y coordinates individually for each old and new position vector, but keep each intended direction as a tuple for easier comparison later
    for i, ((ox, oy), (nx, ny), intended_vec, keypress) in enumerate(zip(old_positions, new_positions, intended_directions, keypresses)):
        # Draw base map for this frame
        fig, ax = vis_map_base(MapClass)

        # =====================================================
        # PREVIOUS movements
        # =====================================================
        # -------- Draw arrows from all past movements --------
        if i > 0:  # Only draw past arrows if there are previous steps
            draw_past_arrows(
                ax,
                old_positions[:i],            # past old positions
                new_positions[:i],            # past new positions
                intended_directions[:i],      # past intended directions (may contain None/empty)
                keypresses[:i],               # past keypresses
                direction_fail_streaks[:i],   # past direction-based fail streaks
                position_fail_streaks[:i]     # past position-based fail streaks
            )

        # -------- Draw markers from all past positions --------
        for j in range(i): # Loop through all previous positions
            px = old_positions[j][0] - 0.5 
            py = old_positions[j][1] - 0.5 

            # plot previous markers slightly smaller and less opaque than current marker (see below)
            ax.plot(px, py, 'o', color=(220/255, 205/255, 125/255), markersize=8, alpha=0.85) 

        # =====================================================
        # CURRENT movement
        # =====================================================
        oxc, oyc = ox - 0.5, oy - 0.5 # Extract x and y components in the center of the old position
        nxc, nyc = nx - 0.5, ny - 0.5 # Extract x and y components in the center of the new position

        # -------- Draw current arrow --------
        draw_current_arrow(ax, ox, oy, nx, ny, intended_vec, keypress, direction_fail_streaks[i], position_fail_streaks[i])

        # -------- Draw current markers --------
        ax.plot(oxc, oyc, 'o', color=(220/255, 205/255, 125/255), markersize=8, zorder=3)
        ax.plot(nxc, nyc, 'o', color=(194/255, 106/255, 119/255), markersize=11, zorder=4)

        # -------- Draw current frame --------
        # Set title for the current frame
        ax.set_title(f"Participant {ID} - Step {i+1}/{total_steps} - {MapClass.__name__} - {Stim}")

        # Save the current frame to the temp directory, add the file path to the frame list, and close the figure
        frame_filename = plot_frame(i, temp_dir, fig, total_steps)
        frame_files.append(frame_filename)

    # =====================================================
    # EXPORT VIDEO
    # =====================================================
    if save_video:
        # Create video from frames
        create_video_from_frames(frame_files, video_filename, fps=1)

        # Clean up temporary frame files and directory
        for f in frame_files:
            os.remove(f)
        os.rmdir(temp_dir)

        print(f"Video saved as: {video_filename}")

def create_video_from_frames(frame_files, output_filename, fps=1):
    """
    Create a video from a list of image frame files.

    Parameters:
        * frame_files: List of str
            - List of file paths to image frames.
        * output_filename: Str
            - The filename for the output video.
        * fps: Int
            - Frames per second for the output video.
        
    Returns:
        None, saves video file.
    """

    # Initialize video writer with first frame's dimensions, assuming all frames are the same size
    first = cv2.imread(frame_files[0])
    height, width, _ = first.shape

    # Define the codec, the codec specifies the video format (Four Character Code mp4v for .mp4)
    fourcc = cv2.VideoWriter_fourcc(*'mp4v')

    # Create VideoWriter object
    vid = cv2.VideoWriter(output_filename, fourcc, fps, (width, height))

    # Write each frame to the video
    for f in frame_files:
        frame = cv2.imread(f)
        vid.write(frame)

    # Release the video writer, finalizing the video file
    vid.release()

# =====================================================
# HELPER FUNCTIONS
# =====================================================
def trim_arrow(x0, y0, x1, y1, tail_trim=0.20, head_trim=0.40):
    """
    Trim arrow ends for better visualization.

    Parameters:
        * x0, y0 : Float
            - Start coordinates of the arrow.
       *  x1, y1 : Float
            - End coordinates of the arrow.
        * tail_trim : Float, optional
            - Distance to trim from the tail of the arrow (default is 0.20 grid units).
        * head_trim : Float, optional
            - Distance to trim from the head of the arrow (default is 0.40 grid units).

    Returns:
        * new_x0, new_y0 : Float 
            - Trimmed coordinates of the tail end of the arrow.
        * new_x1, new_y1 : Float
            - Trimmed coordinates of the head end of the arrow.
    """

    # Compute the difference between end and start points of the arrow to get the arrow vector components
    vx = x1 - x0  # Horizontal component 
    vy = y1 - y0  # Vertical component 

    # Compute the Euclidean length of the arrow vector
    L = np.hypot(vx, vy)  # Equivalent to sqrt(vx**2 + vy**2)

    # If the arrow has zero length (start == end), no trimming is needed
    if L == 0:
        return x0, y0, x1, y1 # Return original coordinates
    
    # Normalize the vector to get direction only (unit vector)
    ux, uy = vx / L, vy / L  # Directional components: length = 1

    # Compute the new coordinates by moving along the unit vector and return
        # Move x and y components (new_x0, new_y0) of the tail of the arrow forward along the direction of the arrow so it starts at the edge of the previous marker rather than in the middle
        # Move x and y components (new_x1, new_y1) of the head of the arrow backward along the direction of the arrow so it ends at the edge of the current marker rather than in the middle
    return (
        x0 + ux * tail_trim, # new_x0
        y0 + uy * tail_trim, # new_y0
        x1 - ux * head_trim, # new_x1
        y1 - uy * head_trim  # new_y1
    )

def calculate_fail_streaks(old_positions, new_positions, intended_directions):
    """
    Calculate fail streaks for a series of movements.

    A fail is defined as no change in position between old and new positions.
    
    Returns two types of fail streaks:
    1. direction_fail_streaks: counts consecutive fails in the same intended direction from the same square
    2. position_fail_streaks: counts consecutive fails from the same square regardless of direction

    Parameters:
        * old_positions: List of tuple
            - List of (x, y) coordinates before movement.
        * new_positions: List of tuple
            - List of (x, y) coordinates after movement.
        intended_directions: List of tuple or None
            - List of (dx, dy) intended movement directions, or None/empty tuple for missing data.

    Returns:
        * tuple: (direction_fail_streaks, position_fail_streaks)
            - direction_fail_streaks: list of fail streaks by direction
            - position_fail_streaks: list of fail streaks by position only
    """
    total_steps = len(old_positions)
    # Initialize lists to track fail streaks for each step, starting at 0
    direction_fail_streaks = [0] * total_steps
    position_fail_streaks = [0] * total_steps

    # Dictionary counting fails per (position, direction)
    direction_fail_counts = {}
    
    # Dictionary counting fails per position only (for missing intended directions)
    position_fail_counts = {}

    for j, ((ox, oy), (nx, ny), intended) in enumerate(zip(old_positions, new_positions, intended_directions)):
        failed = (ox == nx and oy == ny)
        
        # Check if intended direction is missing
        is_missing = intended is None or (isinstance(intended, tuple) and len(intended) == 0)
        
        if failed:
            if is_missing:
                # For missing intended directions, count by position only
                pos_key = (ox, oy)
                if pos_key not in position_fail_counts:
                    position_fail_counts[pos_key] = 0
                position_fail_counts[pos_key] += 1
                position_fail_streaks[j] = position_fail_counts[pos_key]
                direction_fail_streaks[j] = 0  # No direction-based streak
            else:
                # For moves with intended direction, count by (position, direction)
                dir_key = ((ox, oy), intended)
                if dir_key not in direction_fail_counts:
                    direction_fail_counts[dir_key] = 0
                direction_fail_counts[dir_key] += 1
                direction_fail_streaks[j] = direction_fail_counts[dir_key]
                
                # Also update position-based counter
                pos_key = (ox, oy)
                if pos_key not in position_fail_counts:
                    position_fail_counts[pos_key] = 0
                position_fail_counts[pos_key] += 1
                position_fail_streaks[j] = position_fail_counts[pos_key]
        else:
            # Successful move - reset counters for this position/direction
            direction_fail_streaks[j] = 0
            position_fail_streaks[j] = 0
            
            # Reset the appropriate counters for future streaks
            if not is_missing:
                dir_key = ((ox, oy), intended)
                direction_fail_counts[dir_key] = 0
            
            pos_key = (ox, oy)
            position_fail_counts[pos_key] = 0

    return direction_fail_streaks, position_fail_streaks

def create_initial_frame(MapClass, old_positions, participant_id, stim, total_steps, temp_dir):
    """
    Create the initial frame (step 0) of the map visualization.

    Parameters:
        * MapClass: Class 
            - The map class to visualize.
        * old_positions: List of tuple
            - List of (x, y) coordinates; first element is start.
        * participant_id: str/int 
            - Participant identifier for the title.
        * stim: String 
            - Stimulus label for the title.
        * total_steps: Int 
            - Total number of steps in the task.
        * temp_dir: String 
            - Directory to save temporary frame images.
        * frame_files: List
            - List to append the saved frame filename.
    """
    # Draw base map
    fig, ax = vis_map_base(MapClass)

    # Plot start position marker
    start_x, start_y = old_positions[0]
    ax.plot(start_x - 0.5, start_y - 0.5, 'o', color=(220/255, 205/255, 125/255), markersize=12)

    # Set frame title
    ax.set_title(f"Participant {participant_id} - Step 0/{total_steps} - {MapClass.__name__} - {stim}")

    # Save figure
    frame_filename = f"{temp_dir}/frame_0000.png"
    plt.savefig(frame_filename, dpi=100, bbox_inches='tight')
    plt.close(fig)

    print("Created initial frame (step 0).")

    return frame_filename

def draw_past_arrows(ax, old_positions, new_positions, intended_directions, keypresses, direction_fail_streaks, position_fail_streaks):
    """
    Draw arrows representing all past movements on the map,
    including permanent fail-streak labels. Geometry for failed arrows
    matches draw_current_arrow() so repeated fail arrows align exactly.
    Also adds ? markers for past steps with missing intended directions.

    Parameters:
        * ax: matplotlib.axes.Axes
            - The axes to draw arrows on.
        * old_positions: List of tuple
            - List of (x, y) positions before each move.
        * new_positions: List of tuple
            - List of (x, y) positions after each move.
        * intended_directions: List of tuple or None
            - List of intended movement vectors (dx, dy), or None/empty tuple for missing data.
        * keypresses: List of bool
            - List indicating if a key was pressed for each move.
        * direction_fail_streaks: List of int
            - Direction-based fail streak count for each move.
        * position_fail_streaks: List of int
             - Position-based fail streak count for each move.
    """
    # Geometry constants (match draw_current_arrow)
    FAIL_HEAD_WIDTH  = 0.22
    FAIL_HEAD_LENGTH = 0.22
    FAIL_HEAD_TRIM   = 0.48

    NORM_HEAD_WIDTH  = 0.17
    NORM_HEAD_LENGTH = 0.17
    NORM_HEAD_TRIM   = 0.38

    for j, ((px0, py0), (px1, py1), intended, key, dir_streak, pos_streak) in enumerate(
        zip(old_positions, new_positions, intended_directions, keypresses, direction_fail_streaks, position_fail_streaks)):
        
        # Get centered coordinates
        sx0 = px0 - 0.5
        sy0 = py0 - 0.5
        ex0 = px1 - 0.5
        ey0 = py1 - 0.5
        
        # Check if intended direction is missing
        is_missing = intended is None or (isinstance(intended, tuple) and len(intended) == 0)
        
        # For moves with intended direction, draw arrows as normal
        if not is_missing:
            # whether this move failed
            failed = (px0 == px1 and py0 == py1)

            # untrimmed endpoint for failed moves
            if failed:
                dx_int, dy_int = intended
                ex0 = sx0 + dx_int
                ey0 = sy0 + dy_int

            # choose geometry based on fail/success
            if failed:
                head_trim =   FAIL_HEAD_TRIM
                head_width =  FAIL_HEAD_WIDTH
                head_length = FAIL_HEAD_LENGTH
                fill_color = edge_color = (126/255, 41/255, 84/255)
                arrow_z = 3  # slightly above map but below current arrow 
            else:
                head_trim =   NORM_HEAD_TRIM
                head_width =  NORM_HEAD_WIDTH
                head_length = NORM_HEAD_LENGTH
                fill_color = edge_color = (51/255, 117/255, 56/255)
                arrow_z = 2

            # Trim endpoints so arrows start/end at marker edges (consistent)
            sx, sy, ex, ey = trim_arrow(sx0, sy0, ex0, ey0, head_trim=head_trim)

            # draw arrow using selected head geometry
            ax.arrow(sx, sy, ex - sx, ey - sy, head_width=head_width, head_length=head_length,
                fc=fill_color, ec=edge_color, alpha=0.9, zorder=arrow_z)

            # midpoint for labels
            mid_x = sx + (ex - sx) * 0.5
            mid_y = sy + (ey - sy) * 0.5

            # perpendicular for label offset (same scale as draw_current_arrow)
            dx = ex - sx
            dy = ey - sy
            L = np.hypot(dx, dy)

            if L > 0:
                perp_dx = -dy / L * 0.4
                perp_dy = dx / L * 0.4
            else:
                perp_dx = perp_dy = 0

            # Permanent fail-streak label for past failed moves (show if streak > 1)
            if failed and dir_streak > 1:
                ax.text(mid_x + perp_dx, mid_y + perp_dy, str(dir_streak), fontsize=7,
                    fontweight='bold', color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=4,
                    bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))

            # Keypress label ("K"), place opposite the streak label if present
            if key:
                if failed and dir_streak > 1:
                    kx = mid_x - perp_dx
                    ky = mid_y - perp_dy
                else:
                    kx, ky = mid_x, mid_y

                # Add the "K" label with a white circle background for better visibility
                ax.text(kx, ky, 'K', fontsize=5.5, fontweight='bold', 
                   color='black', ha='center', va='center', zorder=4,
                   bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                   edgecolor='black', linewidth=0.8, alpha=0.9))
        
        # For missing intended directions, add ? marker with position-based fail count
        else:
            # Position the ? marker in the top left of the square where the movement was attempted 
            mid_x = (sx0 + ex0) / 2 - 0.3
            mid_y = (sy0 + ey0) / 2 - 0.3
            
            ax.text(mid_x, mid_y, "?", fontsize=6.5, fontweight='bold', 
                    color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=4,
                    bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                    edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))
            
            # Add position-based fail count if streak > 1
            if pos_streak > 1:
                # Position the count on the top right of the square
                ax.text(mid_x + 0.6, mid_y, str(pos_streak), fontsize=6.5,
                       fontweight='bold', color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=4,
                       bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                               edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))
            
            # Add "K" label for keypresses
            if key:
                ax.text(mid_x - 0.1, mid_y + 0.2, 'K', fontsize=5.5, fontweight='bold', 
                       color='black', ha='center', va='center', zorder=4,
                       bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                               edgecolor='black', linewidth=0.8, alpha=0.9))

def draw_current_arrow(ax, ox, oy, nx, ny, intended_vec, keypress, dir_fail_streak, pos_fail_streak):
    """
    Draw the arrow and markers for the current movement step.
    
    If intended_vec is None or an empty tuple, only the ? marker is shown.

    Args:
        ax: matplotlib.axes.Axes
            - Matplotlib Axes to draw on.
        ox, oy: Tuple of Float
            - Old position coordinates.
        nx, ny: Tuple of Float
            - New position coordinates.
        intended_vec: Tuple of Float or None
            - Intended movement vector (dx, dy) or None or empty tuple.
        keypress: Bool 
            - Whether the movement was controlled by keypress.
        dir_fail_streak: Int
            - Direction-based fail streak count for this step.
        pos_fail_streak: Int
            - Position-based fail streak count for this step.
    """
    oxc, oyc = ox - 0.5, oy - 0.5
    nxc, nyc = nx - 0.5, ny - 0.5
    
    # Determine if the movement failed
    failed = (ox == nx and oy == ny)
    
    # Check if intended direction is missing
    is_missing = intended_vec is None or (isinstance(intended_vec, tuple) and len(intended_vec) == 0)

    # Handle missing intended directions
    if is_missing:
        # Position the ? marker in the top left of the square where the movement was attempted
        mid_x = (oxc + nxc) / 2 - 0.3
        mid_y = (oyc + nyc) / 2 - 0.3
        
        ax.text(mid_x, mid_y, "?", fontsize=6.5, fontweight='bold', 
                color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=4,
                bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))
        
        # Add position-based fail count if streak > 1
        if failed and pos_fail_streak > 1:
            # Position the count on the top right of the square
            ax.text(mid_x + 0.6, mid_y, str(pos_fail_streak), fontsize=6.5,
                   fontweight='bold', color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=5,
                   bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                           edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))
        
        # Add "K" label for keypresses
        if keypress:
            ax.text(mid_x + 0.2, mid_y - 0.2, 'K', fontsize=5.5, fontweight='bold', 
                   color='black', ha='center', va='center', zorder=5,
                   bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                           edgecolor='black', linewidth=0.8, alpha=0.9))
        return
    
    # For moves with intended direction, draw arrow as normal
    dx_int, dy_int = intended_vec

    # Determine arrow endpoint
    if failed:
        end_x = oxc + dx_int
        end_y = oyc + dy_int
    else:
        end_x, end_y = nxc, nyc

    # Trim arrow for visualization
    sx, sy, ex, ey = trim_arrow(oxc, oyc, end_x, end_y, head_trim=0.48)

    # Determine arrow color
    if failed:
        fill_color = edge_color = (126/255, 41/225, 84/255)
    else:
        fill_color = edge_color = (51/255, 117/255, 56/255)

    # Draw the arrow
    ax.arrow(sx, sy, ex - sx, ey - sy, head_width=0.22, head_length=0.22,
        fc=fill_color, ec=edge_color, alpha=1.0, zorder=3)

    # Calculate position for labels (middle of the arrow)
    mid_x = sx + (ex - sx) * 0.5
    mid_y = sy + (ey - sy) * 0.5
    
    # Add fail streak count for failed movements with streak > 1
    if failed and dir_fail_streak > 1:
        # Position the number to the side of the arrow
        dx = ex - sx
        dy = ey - sy
        length = np.sqrt(dx**2 + dy**2)
        if length > 0:
            # Perpendicular vector (rotate 90 degrees)
            perp_dx = -dy / length * 0.4
            perp_dy = dx / length * 0.4
            
            label_x = mid_x + perp_dx
            label_y = mid_y + perp_dy
            
            # Add the fail streak count
            ax.text(label_x, label_y, str(dir_fail_streak), fontsize=7, fontweight='bold', 
                   color=(126/255, 41/255, 84/255), ha='center', va='center', zorder=4,
                   bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                           edgecolor=(126/255, 41/255, 84/255), linewidth=1, alpha=0.9))
    
    # Add "K" label for keypresses (position on other side if there's also a streak label)
    if keypress:
        if failed and dir_fail_streak > 1:
            # If there's a streak label, put K on the opposite side
            k_label_x = mid_x - perp_dx
            k_label_y = mid_y - perp_dy
        else:
            # Otherwise put K in the middle
            k_label_x, k_label_y = mid_x, mid_y
        
        # Add the "K" label with a white circle background for better visibility
        ax.text(k_label_x, k_label_y, 'K', fontsize=5.5, fontweight='bold', 
               color='black', ha='center', va='center', zorder=4,
               bbox=dict(boxstyle="circle,pad=0.25", facecolor='white', 
                       edgecolor='black', linewidth=0.8, alpha=0.9))

def plot_frame(i, temp_dir, fig, total_steps):
    frame_filename = f"{temp_dir}/frame_{i+1:04d}.png"
    plt.savefig(frame_filename, dpi=100, bbox_inches='tight')
    plt.close(fig)

    print(f"Created frame {i+1}/{total_steps}")

    return frame_filename