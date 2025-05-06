import os
import shutil

# Define the source file to copy
source_file = '1200_0-0.xml'

# Get a list of all the PNG files in the current directory
png_files = [file for file in os.listdir('.') if file.endswith('.png')]

# Loop through the PNG files and copy the source file for each one
for png_file in png_files:
    new_filename = os.path.splitext(png_file)[0] + '.xml'
    shutil.copy(source_file, new_filename)
    print(f"Copied {source_file} to {new_filename}")

print("Copying complete.")

