import os
import re
import json

# Match beginning of filenames like "1002_..." through "1070_..."
filename_pattern = re.compile(r"(^|/)(10[0-6][0-9]|1070)_(.+)$")

def adjust_file_path(path):
    match = filename_pattern.search(path)
    if match:
        prefix, number_str, rest = match.groups()
        new_number = int(number_str) + 9000
        return path[:match.start()] + f"{prefix}{new_number}_{rest}"
    return path

def process_dfmod_json(filename):
    try:
        with open(filename, 'r', encoding='utf-8') as file:
            data = json.load(file)

        if "Files" not in data:
            print(f"Skipping {filename}: No 'Files' entry found.")
            return

        updated = False
        for i, path in enumerate(data["Files"]):
            new_path = adjust_file_path(path)
            if new_path != path:
                print(f"Updated: {path} -> {new_path}")
                data["Files"][i] = new_path
                updated = True

        if updated:
            with open(filename, 'w', encoding='utf-8') as file:
                json.dump(data, file, indent=4)
            print(f"Saved updates to {filename}")

    except Exception as e:
        print(f"Error processing {filename}: {e}")

def process_all_dfmod_files():
    for file in os.listdir('.'):
        if file.endswith('.dfmod.json'):
            process_dfmod_json(file)

if __name__ == "__main__":
    process_all_dfmod_files()

