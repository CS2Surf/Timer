# PLEASE DO NOT USE THIS, IT IS NOT COMPLETE AND IS AN ACTIVE WORK-IN-PROGRESS. ISSUES HAVE BEEN DISABLED FOR THIS REASON. 
## Please join the Discord: https://discord.cs.surf

# Timer
Core plugin for CS2 Surf Servers. This project is aimed to be fully open-source with the goal of uniting all of CS2 surf towards building the game mode.

# Goals
*Note: This is not definitive/complete and simply serves as a reference for what we should try to achieve. Subject to change.*
Bold & Italics = being worked on by Infra.

- [ ] Database
  - [ ] MySQL database schema ([W.I.P Design Diagram](https://dbdiagram.io/d/CS2Surf-Timer-DB-Schema-6560b76b3be1495787ace4d2))
  - [ ] Plugin auto-create tables for easier setup? 
  - [X] Base database class implementation
- [ ] Maps
  - [X] Implement map info object (DB)
  - [ ] Zoning
    - [X] Start/End trigger touch hooks
    - [X] Load zone information automatically from standardised triggers: https://github.com/CS2Surf/Timer/wiki/CS2-Surf-Mapping 
    - [ ] _**Support for stages (`/rs`, teleporting with `/s`)**_
    - [ ] _**Support for bonuses (`/rs`, teleporting with `/b #`)**_
    - [ ] _**Start/End touch hooks implemented for all zones**_
- [ ] Surf configs
  - [X] Server settings configuration
  - [ ] Plugin configuration
  - [X] Database configuration
- [ ] Timing
  - [X] Base timer class implementation
  - [X] Base timer HUD implementation
  - [X] Prespeed measurement and display
  - [ ] Save/load times
    - [ ] Save/load map personal bests
    - [ ] Save/load map checkpoints
    - [ ] Save/load bonus personal bests
    - [ ] Save/load stage personal bests
  - [ ] Practice Mode implementation
  - [ ] Announce records to Discord
  - [ ] Stretch goal: sub-tick timing
- [ ] Player Data
  - [X] Base player class
  - [ ] Player stat classes
  - [ ] Profile implementation (DB)
  - [ ] Points/Skill Groups (DB)
  - [ ] Player settings (DB)
- [ ] Run replays
- [ ] Style implementation (SW, HSW, BW)
- [ ] Paint (?)
