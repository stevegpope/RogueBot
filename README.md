RogueBot - play rogue automatically, now in parallel!

This is the early implementation, so it is FAR from perfect. The purpose of this project is to inspire others to make better AI for the game.

Instructions
=============
- Compile .sln
- cd bin\Debug\net10.0-windows
- .\RogueBot.exe <number of processes>

It will:
- launch rogue54.exe (or connect to an existing one)
- read the console
- send key strokes to the console

Run RogueWatcher.exe -r to get a list of rogues and what level they are at, -p <pid> to see the current map of a rogue process 

Current Features
=================
- auto-explore
- pick-up items
- fight monsters - the plan is to back up into a corridoor to funnel the enemies and wait for them. If they don't show up we go throw something at them and try again.
- drink potions found until we know what they are
- read scrolls found until we know what they are
- wear armor found if it appears to be better
- wield new weapons found
- eat when hungry (BUG: often he eats the second time, about 5 seconds after he first gets hungry and tries to eat. This isn't really a problem, because it actually saves us on food to wait longer)

NOTES
=================
- On startup the bot does an inventory check, pressing "i" and waiting for the game inventory to be opened. If it appears stuck on start, manually opening the inventory gets it moving.

