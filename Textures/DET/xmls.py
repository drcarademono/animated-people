import os
import xml.etree.ElementTree as ET

def generate_xml_files(directory):
    for filename in os.listdir(directory):
        if filename.endswith("-0.png"):
            # Extract the base filename without the extension
            base_name = os.path.splitext(filename)[0]
            xml_filename = f"{base_name}.xml"
            
            # Create the XML structure
            root = ET.Element("info")
            scale_x = ET.SubElement(root, "scaleX")
            scale_x.text = "0.4924"
            scale_y = ET.SubElement(root, "scaleY")
            scale_y.text = "0.4924"
            
            # Write the XML to a file
            tree = ET.ElementTree(root)
            output_path = os.path.join(directory, xml_filename)
            tree.write(output_path, encoding="utf-8", xml_declaration=True)
            print(f"Generated {xml_filename}")

# Replace 'your_folder_path' with the path to your folder containing the PNGs
your_folder_path = "."
generate_xml_files(your_folder_path)

