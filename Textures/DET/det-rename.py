import os
import re

# Match files that start with a number between 1002 and 1070 followed by an underscore
pattern = re.compile(r"^(10[0-6][0-9]|1070)_(.+)$")

def process_filenames():
    for filename in os.listdir("."):
        match = pattern.match(filename)
        if match:
            number_str, rest = match.groups()
            number = int(number_str)
            new_number = number + 9000
            new_filename = f"{new_number}_{rest}"
            print(f"Renaming: {filename} -> {new_filename}")
            os.rename(filename, new_filename)

if __name__ == "__main__":
    process_filenames()

