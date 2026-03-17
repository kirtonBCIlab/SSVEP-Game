import re
import os

def extract_file_components(data_path):
    """
    Extract structured components from a file path following the naming pattern:
    ..._<P#>_<C#S# or BW>_..._M#....
    
    Returns:
        dict with keys: participant, stim_name, map, file
    """
    
    filename = os.path.splitext(os.path.basename(data_path))[0]
    parts = filename.split('_')

    # --- Extract participant code (P##) ---
    p_code = next((p for p in parts if re.fullmatch(r'P\d+', p)), None)

    # --- Extract stimulus code ---
    stim_code = next((s for s in parts if re.fullmatch(r'(C\d+S\d+|BW)', s)), None)
    if stim_code is None:
        return None  # Can't parse this file format

    if stim_code == "BW":
        stim_name = "Standard"
    else:
        contrast_num, size_num = re.match(r'C(\d+)S(\d+)', stim_code).groups()
        stim_name = f"Contrast{contrast_num}Size{size_num}"

    # --- Extract map number (M#) ---
    map_part = next((m for m in parts if re.fullmatch(r'M\d+', m)), None)
    map_number = int(map_part[1:]) if map_part else None

    return {
        'participant': p_code,
        'stim_name': stim_name,
        'map': map_number,
        'file': filename
    }
