# EVERYONE IS WELCOME TO BUILD UP ON THIS PROJECT AND CONTRIBUTE. ISSUES ARE DISABLED FOR THE TIME BEING 
## Please join the Discord: https://discord.cs.surf

# Timer
Core plugin for CS2 Surf Servers. This project is aimed to be fully open-source with the goal of uniting all of CS2 surf towards building the game mode.
<br>
<details> 
  <summary>Center HUD Speedometer</summary>
  <p>Different time formatting is available in the code base but not implemented for players to change it themselves. Refer to <strong>PlayerTimer.TimeFormatStyle</strong> in codebase</p>
  <ul>
    <li><strong>Map</strong>
      <p><em>Shown while player starts their Map Run from the map start zone (!r)</em></p>
      <details>
        <summary>Zone name + Start speed</summary>
        <p>Map Run mode:</p>
        <img src="https://i.imgur.com/IqUL067.png" alt="Map Start Zone">
        <img src="https://i.imgur.com/ITooApq.png" alt="Map Start Zone exit">
      </details>
    </li>
    <li><strong>Checkpoints</strong>
      <p><em>Only shown during Map Run after exiting a Checkpoint/Stage zone</em></p>
      <details>
        <summary>Zone name + Start speed</summary>
        <p>Checkpoint comparison:</p>
        <img src="https://i.imgur.com/recf26f.png" alt="Checkpoint Start Zone exit">
      </details>
    </li>
    <li><strong>Stages</strong>
      <p><em>Only shown while in Stage Mode, accessed through !s X or !stage X commands</em></p>
      <details>
        <summary>Zone name + Start speed</summary>
        <p>Stage Run mode:</p>
        <img src="https://i.imgur.com/Zi3HN2b.png" alt="Stages Start Zone">
        <img src="https://i.imgur.com/uYyumVJ.png" alt="Stages Start Zone exit">
      </details>
    </li>
    <li><strong>Bonuses</strong>
      <p><em>Only shown while in Bonus Mode, accessed through !b X or !bonus X commands</em></p>
      <details>
        <summary>Zone name + Start speed</summary>
        <p>Bonus Run mode:</p>
        <img src="https://i.imgur.com/Tlmdq9r.png" alt="Bonuses Start Zone">
        <img src="https://i.imgur.com/Rfm9qG4.png" alt="Bonuses Start Zone exit">
      </details>
    </li>
  </ul>
</details>

<details> 
  <summary>Replays</summary>
  <p>Currently only accessible through the <strong>!spec</strong> command and cycling the players. Different time formatting is available in the code base but not implemented for players to change it themselves. Refer to <strong>PlayerTimer.TimeFormatStyle</strong> in codebase</p>
  <p>Replays are saved for all types of runs Map/Stage/Bonus (and future Styles) regardless if they are a World Record or just a Personal Best. No functionality is implemented for replaying PB replays yet, feel free to add and Pull Request it</p>
  <ul>
    <li><strong>Map</strong>
      <details>
        <summary>Spectating Map Replay</summary>
        <p>Map Run:</p>
        <img src="https://i.imgur.com/gZutBkS.png" alt="Map Run Replay">
      </details>
    </li>
    <li><strong>Stages</strong>
      <details>
        <summary>Spectating Stage Replay</summary>
        <p>Stage Run:</p>
        <img src="https://i.imgur.com/tL7kM1l.png" alt="Stages Run Replay">
      </details>
    </li>
    <li><strong>Bonuses</strong>
      <p>Bonus Replays are also available but no screenshots at the time of writing.</p>
    </li>
    <li><strong>Scoreboard</strong>
      <details>
        <summary>Currently available replays for the map</summary>
        <p>Scoreboard:</p>
        <img src="https://i.imgur.com/RNTTFgi.png" alt="Scoreboard showing all available Replays">
      </details>
    </li>
  </ul>
</details>


<details> 
  <summary>Chat Messages</summary>
  <ul>
    <li><strong>Map Run</strong>
      <details>
        <summary>Improving a Record</summary>
        <p>Timer sends a chat message to all players upon a player beating the Map Record. Missing it sends a message only to the player:</p>
        <img src="https://i.imgur.com/ggCNjZ8.png" alt="Beating a Map record">
      </details>
      <details>
        <summary>Checkpoint Comparison</summary>
        <p>Timer sends a chat message the player comparing their PB checkpoint times with the current run (value in brackets after the time indicate the Speed):</p>
        <img src="https://i.imgur.com/ts4FfhY.png" alt="Checkpoints Comparison">
      </details>
    </li>
    <li><strong>Stage Records</strong>
      <details>
        <summary>New Stage record and improving Stage record</summary>
        <p>Timer sends a chat message to all players upon a player beating a Stage/Bonus/Map record. Different scenarios for missed/comparing times are also available and shown in chat but only to the player who is doing the run:</p>
        <img src="https://i.imgur.com/MNehNmv.png" alt="Stage Records and comparisons">
      </details>
    </li>
    <li><strong>QOL</strong>
      <details>
        <summary>Player Connected + Map Info (!mi / !tier)</summary>
        <p>LL is used for Local development and testing:</p>
        <img src="https://i.imgur.com/JtHwYnx.png" alt="Player Connected + Map Info">
      </details>
      <details>
        <summary>Player Rank</summary>
        <p>Displays the rank of the player on the current map:</p>
        <img src="https://i.imgur.com/4BXJjMv.png" alt="Player Rank">
      </details>
    </li>
  </ul>
</details>

<details> 
  <summary>Player Commands</summary>
      <p>We recommend making binds using the <strong>Console</strong> commands, chat commands may flood the server and not always work.</p>
  <ul>
    <li><strong>Saveloc (Practice Mode)</strong>
      <details>
        <summary>Save the current location</summary>
        <p>Chat: !saveloc</p>
        <p>Console: css_saveloc</p>
      </details>
      <details>
        <summary>Teleport to the last saved location</summary>
        <p>Chat: !tele</p>
        <p>Console: css_tele</p>
      </details>
      <details>
        <summary>Teleport to the previous saved location</summary>
        <p>Chat: !teleprev</p>
        <p>Console: css_teleprev</p>
      </details>
      <details>
        <summary>Teleport to the next saved location</summary>
        <p>Chat: !telenext</p>
        <p>Console: css_telenext</p>
      </details>
    </li>
  </ul>
  <ul>
    <li><strong>Spectate</strong>
      <details>
        <summary>Enter Spectator Mode</summary>
        <p>Chat: !spec</p>
        <p>Console: css_spec</p>
      </details>
      <details>
        <summary>Exiting Spectator Mode</summary>
        <p>No command currently available to go back to Play Mode (time may NOT be reset and you will loose your progress post entering Spectator Mode)</p>
        <p>Open team choosing menu <strong>M</strong> and select CT</p>
      </details>
    </li>
  </ul>
</details>
</br>

## 🔗 Dependencies
- [`CounterStrikeSharp`](https://github.com/roflmuffin/CounterStrikeSharp) - **required** minimum version [v1.0.339](https://github.com/roflmuffin/CounterStrikeSharp/releases/tag/v1.0.337).
- [`SurfTimer.Shared`](https://github.com/tslashd/SurfTimer.Shared) – **required** shared library for DTOs, entities, and database integration.  
- [`SurfTimer.Api`](https://github.com/tslashd/SurfTimer.Api) – *optional* REST API for faster, centralized communication with the database.

# Main list with tasks (more details can be found [here](https://github.com/CS2Surf/Timer/blob/dev/TODO)):
*Note: This is not definitive/complete and simply serves as a reference for what we should try to achieve. Subject to change.*
Bold & Italics = being worked on.
- [ ] Database
  - [X] MySQL database schema ([Design Diagram](https://dbdiagram.io/d/Copy-of-CS2Surf-Timer-DB-Schema-6582e6e456d8064ca06328b9))
  - [ ] Plugin auto-create tables for easier setup? 
  - [X] Base database class implementation
- [X] Maps
  - [X] Implement map info object (DB)
  - [X] Zoning
    - [X] Hook zones from map triggers
      - [X] Map start/end zones
      - [X] Stage zones
      - [X] Checkpoint zones (this is each stage for a Staged map)
      - [X] Bonus zones
    - [X] Support for stages/checkpoints
      - [X] Hook to their start/end zones
      - [X] Save/Compare checkpoint times
      - [X] Save Stage times
    - [X] Support for bonuses
      - [X] Hook to their start/end zones
      - [X] Save Bonus times
    - [X] Start/End trigger touch hooks
    - [X] Load zone information automatically from standardised triggers: https://github.com/CS2Surf/Timer/wiki/CS2-Surf-Mapping 
    - [X] Support for stages (`/rs`, teleporting with `/s`)
    - [X] Support for bonuses (`/rs`, teleporting with `/b #`)
    - [X] Start/End touch hooks implemented for all zones
- [ ] Surf configs
  - [X] Server settings configuration
  - [ ] Plugin configuration
  - [X] Database configuration
- [X] Timing
  - [X] Base timer class implementation
  - [X] Base timer HUD implementation
  - [X] Prespeed measurement and display
  - [X] Save/load times
    - [x] Map times
    - [x] Checkpoint times
    - [X] Stage times
    - [X] Bonus times
  - [X] Practice Mode implementation
  - [ ] Announce records to Discord
  - [ ] Stretch goal: sub-tick timing
- [ ] Player Data
  - [X] Base player class
  - [X] Player stat classes
  - [X] Profile implementation (DB)
  - [ ] Points/Skill Groups (DB)
  - [ ] Player settings (DB)
- [x] Replays
   - [x] Personal Best - Data for the PB replays is saved but no functionality to replay them yet is available
      - [x] Map Record
      - [X] Stage Record
      - [X] Bonus Record
   - [x] World Record
      - [X] Map Record
      - [X] Stage Record
      - [X] Bonus Record
- [ ] Style implementation (SW, HSW, BW)
- [ ] Paint (?)
- [x] API Integration (Repo can be found [here](https://github.com/tslashd/SurfTimer.Api))
