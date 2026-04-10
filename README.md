RogueBot - play rogue automatically
This is the early implementation, so it is FAR from perfect. The purpose of this project is to inspire others to make better AI for the game.

Instuctions
=============
- Compile .sln
- cd bin\Debug\net10.0-windows
- .\RogueBot.exe

It will:
- launch rogue54.exe (or connect to an existing one)
- read the console
- send key strokes using SendKeys.SendWait

Current Features
=================
- auto-explore
- pick-up items
- fight monsters
- drink potions found
- read scrolls found
- wear armor found
- wield weapons found
- eat when hungry

NOTES
=================
- On startup the bot does an inventory check, pressing "i" and waiting for the game inventory to be opened. If it appears stuck on start, manually opening the inventory gets it moving.

