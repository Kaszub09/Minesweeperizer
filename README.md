# Minesweeperizer
Your image will look like from minesweeper.

Refer to Examples folder for, well, examples.

## Usage
* Works best with white/black only images. 
* You can choose multiple files.
* You can create your own themes. Just put files 0-10.png in a folder with any name, and put that folder in "minesweeper" folder,
which should be in the same place as .exe. It is checked when opening program, and all avaible themes are listed.
* Recognised image - image before applying minesweeper tiles. White - "unclicked", black - "clicked". 
First image is scaled down to widthxheight, then average value of the pixel is calculated from RGB colors, then is set as white if value >127, or black otherwise.
* You can also choose to draw flags on every "unclicked" space, on ones neighbouring "clicked" or don't draw them at all.
* Minesweeperized image is save in the same folder as source, 
with " (minesweeperized)" added at the end of the filename (overwriting if such file already exist).
* Have fun!
