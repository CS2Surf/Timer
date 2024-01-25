# Timer
Core plugin for CS2 Surf Servers. This project is aimed to be fully open-source with the goal of uniting all of CS2 surf towards building the game mode.

# Goals
*Note: This is not definitive/complete and simply serves as a reference for what we should try to achieve. Subject to change.*

- [ ] Data storage
  - [ ] MySQL database schema ([W.I.P Design Diagram](https://dbdiagram.io/d/CS2Surf-Timer-DB-Schema-6560b76b3be1495787ace4d2))
  - [ ] Plugin auto-create tables for easier install? 
- [ ] Zoning
  - [x] Hook zones from map triggers
    - [x] Map start/end zones
    - [x] Stage zones
    - [x] Checkpoint zones (this is each stage for a Staged map)
    - [x] Bonus zones
  - [x] Support for stages/checkpoints
    - [x] Hook to their start/end zones
    - [x] Save/Compare checkpoint times
    - [ ] Save Stage times
  - [x] Support for bonuses
    - [x] Hook to their start/end zones
    - [ ] Save Bonus times
  - [ ] Load zone information for official maps from CS2 Surf upstream? (Probably make this optional)
  - [ ] Support for custom zoning (Draw in-game similar to CSGO Surftimer?)
- [ ] Timing
  - [ ] Implement timer HUD (similar to WST)
  - [ ] Save/load times from the database
    - [x] Map times
    - [ ] Stage times
    - [ ] Bonus times
    - [x] Checkpoint times
  - [x] Practice Mode
  - [ ] Announce records to Discord
- [ ] Player Data
  - [ ] Profiles
  - [ ] Points/Skill Groups
- [x] Replays - Not tracking Stage/Bonus times but Replay functionality for them is there
   - [x] Personal Best 
      - [x] Map Record
      - [ ] Stage Record
      - [ ] Bonus Record
   - [x] World Record
      - [X] Map Record
      - [ ] Stage Record
      - [ ] Bonus Record
- [ ] Angle style implementation
- [ ] Paint
