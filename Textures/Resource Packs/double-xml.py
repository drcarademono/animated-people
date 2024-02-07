import xml.etree.ElementTree as ET
import glob

# Pattern to match all XML files in the current folder
xml_files_pattern = './*.xml'

def double_scale_values(xml_file):
    # Parse the XML file
    tree = ET.parse(xml_file)
    root = tree.getroot()
    
    # Find the scaleX and scaleY elements and double their values
    for scale_element in root.findall('.//scaleX'):
        current_value = float(scale_element.text)
        scale_element.text = str(current_value * .5)
        
    for scale_element in root.findall('.//scaleY'):
        current_value = float(scale_element.text)
        scale_element.text = str(current_value * .5)
    
    # Write the modified XML back to the file
    tree.write(xml_file)

# Iterate over all XML files in the current folder and apply the changes
for xml_file in glob.glob(xml_files_pattern):
    double_scale_values(xml_file)
    print(f"Processed {xml_file}")

