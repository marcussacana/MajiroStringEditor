
## MajiroStringEditor - v1.0

A tool to transalte the MJO files from Majiro Engine,  
Tested with "If You Love Me, Then Say So!"

### Tags
This tags is someting automatically converted to the Majiro script  
and don't represent an real tag of this engine.
- **\n** Linebreak
- **[wait]** Wait for the user to click to show the remaining text
- **[clear]** Clears the text box
- **[line]** Marks where the character name ends and where the character's dialogue begins

### Samples Lines
- Splitting the dialogue in two parts
	`Name 1[line]Dialogue 1[wait][clear]Name 2[line]Dialogue 2`
- Spliting the line in two
	`Line 1\nLine 2`


### Extract/Repack
This tool don't include .arc extractor or repacker,  
to this you can use the [arc_conv](https://github.com/amayra/arc_conv)
- **Extract:** Drag&Drop the .arc
- **Repack:**  arc_conv.exe --pack majiro .\scenario .\scenario.arc 3  
	- **.\scenario** = Input Directory
	- **.\scenario.arc** = Output Archive
	- **3 = Arc version** (1, 2 or 3)
