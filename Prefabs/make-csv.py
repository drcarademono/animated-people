import os
import csv

# Define the CSV file name
csv_file = "AnimatedPeople.csv"

# Define the fields for the CSV
fields = ["Archive", "Record", "SecondsPerFrame", "DelayMin", "DelayMax", "RepeatMin", "RepeatMax"]

# Function to process prefab files and extract necessary values
def process_prefab(file_path):
    with open(file_path, 'r') as file:
        lines = file.readlines()
        data = {}
        for line in lines:
            if 'Archive:' in line:
                data['Archive'] = int(line.split(':')[1].strip())
            elif 'Record:' in line:
                data['Record'] = int(line.split(':')[1].strip())
            elif 'SecondsPerFrame:' in line:
                data['SecondsPerFrame'] = float(line.split(':')[1].strip())
            elif 'DelayMin:' in line:
                data['DelayMin'] = float(line.split(':')[1].strip())
            elif 'DelayMax:' in line:
                data['DelayMax'] = float(line.split(':')[1].strip())
            elif 'RepeatMin:' in line:
                data['RepeatMin'] = int(line.split(':')[1].strip())
            elif 'RepeatMax:' in line:
                data['RepeatMax'] = int(line.split(':')[1].strip())
        
        # Ensure all keys exist with default values if not found
        return {
            "Archive": data.get("Archive", 0),
            "Record": data.get("Record", 0),
            "SecondsPerFrame": data.get("SecondsPerFrame", 0.1),
            "DelayMin": data.get("DelayMin", 0),
            "DelayMax": data.get("DelayMax", 0),
            "RepeatMin": data.get("RepeatMin", 0),
            "RepeatMax": data.get("RepeatMax", 0)
        }

# Function to find all *.prefab files and process them
def find_prefabs_and_write_csv():
    prefab_files = [f for f in os.listdir('.') if f.endswith('.prefab')]
    rows = []
    for prefab_file in prefab_files:
        row = process_prefab(prefab_file)
        if row:
            rows.append(row)
    
    with open(csv_file, 'w', newline='') as csvfile:
        writer = csv.DictWriter(csvfile, fieldnames=fields)
        writer.writeheader()
        for row in rows:
            writer.writerow(row)

# Run the function to process prefabs and write the CSV
find_prefabs_and_write_csv()

