# PLEASE DO NOT USE THIS, IT IS NOT COMPLETE AND IS AN ACTIVE WORK-IN-PROGRESS. ISSUES HAVE BEEN DISABLED FOR THIS REASON. 
## Please join the Discord: https://discord.cs.surf

# Timer
Core plugin for CS2 Surf Servers. This project is aimed to be fully open-source with the goal of uniting all of CS2 surf towards building the game mode.

# Goals
*Note: This is not definitive/complete and simply serves as a reference for what we should try to achieve. Subject to change.*
Bold & Italics = being worked on.

- [ ] Database
  - [ ] MySQL database schema ([W.I.P Design Diagram](https://dbdiagram.io/d/CS2Surf-Timer-DB-Schema-6560b76b3be1495787ace4d2))
  - [ ] Plugin auto-create tables for easier setup? 
  - [X] Base database class implementation
- [ ] Maps
  - [X] Implement map info object (DB)
  - [ ] Zoning
    - [X] Hook zones from map triggers
      - [X] Map start/end zones
      - [X] Stage zones
      - [X] Checkpoint zones (this is each stage for a Staged map)
      - [X] Bonus zones
    - [X] Support for stages/checkpoints
      - [X] Hook to their start/end zones
      - [X] Save/Compare checkpoint times
      - [ ] Save Stage times
    - [X] Support for bonuses
      - [X] Hook to their start/end zones
      - [X] Save Bonus times
    - [X] Start/End trigger touch hooks
    - [X] Load zone information automatically from standardised triggers: https://github.com/CS2Surf/Timer/wiki/CS2-Surf-Mapping 
    - [X] Support for stages (`/rs`, teleporting with `/s`)
    - [X] Support for bonuses (`/rs`, teleporting with `/b #`)
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
    - [x] Map times
    - [x] Checkpoint times
    - [ ] Stage times
    - [X] Bonus times
  - [X] Practice Mode implementation
  - [ ] Announce records to Discord
  - [ ] Stretch goal: sub-tick timing
- [ ] Player Data
  - [X] Base player class
  - [ ] **_Player stat classes_**
  - [ ] Profile implementation (DB)
  - [ ] Points/Skill Groups (DB)
  - [ ] Player settings (DB)
- [x] Replays - Not tracking Stage/Bonus times but Replay functionality for them is there
   - [x] Personal Best 
      - [x] Map Record
      - [ ] Stage Record
      - [ ] Bonus Record
   - [x] World Record
      - [X] Map Record
      - [ ] Stage Record
      - [ ] Bonus Record
- [ ] Style implementation (SW, HSW, BW)
- [ ] Paint (?)
