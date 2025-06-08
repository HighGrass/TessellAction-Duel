# TessellAction-Duel: Multiplayer Turn-Based Strategy Game Network Architecture Report

- **Author:** Martim Vicente
- **Project:** TessellAction-Duel
- **Framework:** Unity with Photon PUN2
- **Game Type:** Real-time multiplayer turn-based strategy

## Executive Summary

TessellAction-Duel is a networked multiplayer turn-based strategy game built using Unity and Photon PUN2. The project implements a networking architecture featuring real-time matchmaking, turn-based gameplay mechanics, user authentication with backend integration, anti-cheat duplicate account prevention, and disconnection handling. The game supports 1v1 matches with automatic player pairing, persistent user statistics stored in MongoDB, and a 30-second turn timer system.

## Core Architecture Overview

### Network Infrastructure
The game utilizes Photon PUN2 as the primary networking solution, providing:
- **Real-time multiplayer communication** via Photon Cloud
- **Room-based matchmaking** with automatic player pairing (max 2 players)
- **RPC (Remote Procedure Call)** system for turn management and piece movement
- **Custom properties** for player identification and duplicate account prevention
- **Master Client** authority for game state validation and turn management
- **PhotonView synchronization** for game piece states and positions

### Authentication & Backend Integration
- **RESTful API integration** with Node.js/Express backend hosted on Render.com
- **JWT token-based authentication** with bcrypt password hashing
- **MongoDB Atlas database** for persistent user data storage
- **Encrypted local data storage** using XOR cipher for session persistence
- **Automatic token validation** on application startup

## Code Architecture

### Core Components

#### 1. AuthManager (Singleton Pattern)
**Purpose:** Centralized authentication and user data management
**Key Features:**
- JWT token lifecycle management with automatic validation
- RESTful API communication with backend (login, register, stats update)
- XOR encrypted local data persistence for session management
- User statistics tracking (GlobalScore, GamesPlayed, GamesWon)
- Event-driven architecture for UI updates (OnStatsUpdated, OnUserChanged)

```csharp
public static AuthManager Instance { get; private set; }
public event Action OnStatsUpdated;
public event Action OnUserChanged;
public static string AuthToken { get; private set; }
public static string UserId { get; private set; }
public static int GlobalScore { get; private set; }
```

#### 2. SimplePunLauncher (Network Manager)
**Purpose:** Photon network connection and matchmaking orchestration
**Key Features:**
- Automatic Photon connection management with ConnectUsingSettings()
- Room creation and random room joining for matchmaking
- Anti-duplication system preventing same account from playing together
- Player custom properties setup (GameUserId, Username)
- Scene transition management and disconnection handling
- DontDestroyOnLoad singleton pattern

```csharp
public class SimplePunLauncher : MonoBehaviourPunCallbacks
{
    public bool IsInLobby { get; private set; }
    public static SimplePunLauncher Instance;
    private void SetupPlayerProperties()
    private bool CheckForDuplicateUsers()
    private void HandlePlayerDisconnectedDuringGame()
}
```

#### 3. TurnManager (Game State Controller)
**Purpose:** Turn-based gameplay logic and synchronization
**Key Features:**
- Turn sequence management via SyncTurnRpc
- 30-second turn timer with automatic switching on timeout
- Master Client authority for turn validation
- Win/loss condition evaluation with point system (+50 win, -25 loss)
- Game result processing and backend statistics update
- Coroutine-based timer management

```csharp
public class TurnManager : MonoBehaviourPunCallbacks
{
    public int CurrentTurnIndex { get; private set; }
    public float turnTimeLimit = 30f;
    public float CurrentTurnTimeRemaining => currentTurnTimeRemaining;
    public bool IsMyTurn => PhotonNetwork.LocalPlayer.ActorNumber == CurrentTurnIndex;
    public bool IsGameActive => isGameActive;
}
```

#### 4. Piece (Game Entity)
**Purpose:** Individual game piece logic and network synchronization
**Key Features:**
- PhotonView with IPunObservable for state synchronization
- RPC-based movement execution (ExecuteMoveRpc, UpdateOwnerStateRpc)
- Owner-based interaction permissions with material visualization
- Jump/capture mechanics with neighbor calculation
- Visual feedback system (hover, selection, highlighting)
- Ownership transfer management via PhotonView.TransferOwnership

```csharp
public class Piece : MonoBehaviourPun, IPunObservable
{
    public int OwnerId => ownerIdInternal;
    public bool IsInteractable => isInteractableInternal;
    public Vector3 CurrentPosition { get; private set; }
    public List<Piece> GetPossibleMoves()
    public void SetOwnerState(int newOwnerActorNumber, bool newInteractable)
}
```

## Network Diagram

```
Client A ←→ Photon Cloud ←→ Client B
    ↓           ↓           ↓
Backend API ←→ Database ←→ Backend API
```

### Data Flow Architecture

1. **Authentication Flow:**
   ```
   Client → Backend API → Database → JWT Token → Client
   ```

2. **Matchmaking Flow:**
   ```
   Client → Photon Lobby → Room Creation/Join → Game Start
   ```

3. **Gameplay Flow:**
   ```
   Player Input → RPC → Master Client → Validation → All Clients
   ```

## Problem-Solution Analysis

### Problem 1: Account Exploitation for Point Farming
**Issue:** Players could use the same account on multiple instances to play against themselves, exploiting the point system (win +50, lose -25) for infinite point farming.

**Root Cause:** No validation to prevent multiple instances of the same user account from joining the same game room.

**Solution Implemented:**
```csharp
private bool CheckForDuplicateUsers()
{
    string myUserId = AuthManager.UserId;
    foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
    {
        if (player.IsLocal) continue;
        if (player.CustomProperties.TryGetValue("GameUserId", out object otherUserId))
        {
            if (otherUserIdStr == myUserId) return true;
        }
    }
    return false;
}
```

**Implementation Details:**
- Store UserId in Photon custom properties during connection
- Real-time validation on room join and player entry events
- Automatic room ejection with user-friendly error message
- Event-driven property updates on login/logout to handle account switching

### Problem 2: Game Stalling and Poor User Experience
**Issue:** Players could deliberately stall their turns indefinitely to frustrate opponents, leading to abandoned games.

**Root Cause:** No time limit enforcement on player turns.

**Solution Implemented:**
```csharp
private IEnumerator TurnTimerCoroutine()
{
    while (currentTurnTimeRemaining > 0 && isGameActive)
    {
        yield return new WaitForSeconds(1f);
        currentTurnTimeRemaining -= 1f;
    }

    if (isGameActive && PhotonNetwork.IsMasterClient)
    {
        SwitchToNextTurn(); // Automatic turn switching
    }
}
```

**Implementation Details:**
- 30-second turn timer with visual countdown
- Master Client authority for turn switching to prevent conflicts
- Coroutine-based timer management with proper cleanup
- UI integration with color-coded warnings (yellow at 10s, red at 5s)

### Problem 3: Mid-Game Disconnections and Unfair Outcomes
**Issue:** When players disconnected mid-game, the remaining player was left in limbo with no clear resolution or point attribution.

**Root Cause:** No automated handling of player disconnections during active gameplay.

**Solution Implemented:**
```csharp
public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
{
    if (SceneManager.GetActiveScene().name == "GameScene")
    {
        // Remaining player gets automatic win
        AuthManager.Instance.SendGameResult("win", 50);
        StartCoroutine(ReturnToMatchmakingAfterDisconnect());
    }
}
```

**Implementation Details:**
- Scene-based disconnection detection (GameScene vs MatchmakingMenu)
- Automatic win assignment (+50 points) to remaining player
- Automatic loss assignment to disconnected player (when they reconnect)
- Seamless return to matchmaking queue after 1-second delay

### Problem 4: Session Management and Authentication Persistence
**Issue:** Users had to re-login every time they opened the application, creating poor user experience.

**Root Cause:** No local session persistence or automatic token validation.

**Solution Implemented:**
```csharp
private void Awake()
{
    if (File.Exists(savePath))
    {
        string json = File.ReadAllText(savePath);
        string decryptedJson = EncryptDecrypt(json); // XOR cipher
        SavedAuthData data = JsonUtility.FromJson<SavedAuthData>(decryptedJson);
        AuthToken = data.token;
        UserId = data.userId;
        StartCoroutine(VerifyTokenCoroutine()); // Automatic validation
    }
}
```

**Implementation Details:**
- XOR cipher encryption for local credential storage
- Automatic token validation on application startup
- JWT token lifecycle management with backend verification
- Graceful fallback to login screen if token is invalid

### Problem 5: Network State Synchronization Issues
**Issue:** Game state could become desynchronized between players, especially during login/logout scenarios.

**Root Cause:** Photon custom properties not updating when users changed accounts.

**Solution Implemented:**
```csharp
public event Action OnUserChanged;

private void OnUserChanged()
{
    SetupPlayerProperties(); // Update Photon properties
}

// Called on login, logout, and register
OnUserChanged?.Invoke();
```

**Implementation Details:**
- Event-driven architecture for user state changes
- Automatic Photon property updates on account switching
- Real-time synchronization of user identification across all clients
- Proper cleanup of properties on logout

## Anti-Cheat Implementation

### Duplicate Account Prevention
**Problem:** Players using multiple instances of the same account to manipulate scoring
**Solution:** Real-time duplicate detection system using Photon custom properties

```csharp
private bool CheckForDuplicateUsers()
{
    if (string.IsNullOrEmpty(AuthManager.UserId))
        return false;

    string myUserId = AuthManager.UserId;
    foreach (var player in PhotonNetwork.CurrentRoom.Players.Values)
    {
        if (player.IsLocal) continue;

        if (player.CustomProperties.TryGetValue("GameUserId", out object otherUserId))
        {
            string otherUserIdStr = otherUserId?.ToString();
            if (!string.IsNullOrEmpty(myUserId) && !string.IsNullOrEmpty(otherUserIdStr)
                && otherUserIdStr == myUserId)
            {
                return true;
            }
        }
    }
    return false;
}
```

**Implementation Details:**
- User ID stored in Photon custom properties via SetupPlayerProperties()
- Real-time validation on OnJoinedRoom and OnPlayerEnteredRoom with delay for synchronization
- Automatic room ejection with ShowDuplicateUserError() message
- Event-driven property updates via OnUserChanged event on login/logout

## Technical Challenges Solved

### Challenge 1: Photon Property Synchronization Timing
**Problem:** Custom properties were not immediately available when players joined rooms, causing false positives in duplicate detection.

**Solution:** Implemented delayed validation with coroutines:
```csharp
private IEnumerator CheckForDuplicatesAfterDelay(float delay)
{
    yield return new WaitForSeconds(delay);
    if (!PhotonNetwork.InRoom) yield break;

    if (CheckForDuplicateUsers())
    {
        ShowDuplicateUserError();
        PhotonNetwork.LeaveRoom();
    }
    else
    {
        CheckForGameStart();
    }
}
```

### Challenge 2: Master Client Authority vs Distributed Validation
**Problem:** Turn validation needed to be authoritative but also responsive to all players.

**Solution:** Master Client authority with RPC distribution:
```csharp
[PunRPC]
private void RequestMoveRpc(int movingPieceViewID, int destinationPieceViewID, PhotonMessageInfo info)
{
    if (!PhotonNetwork.IsMasterClient) return; // Authority check
    if (info.Sender.ActorNumber != CurrentTurnIndex) return; // Turn validation

    // Validate move logic...
    photonView.RPC("SyncTurnRpc", RpcTarget.All, nextTurnIndex);
}
```

### Challenge 3: Memory Leaks from Event Subscriptions
**Problem:** Event subscriptions in singleton objects were not being cleaned up, causing memory leaks.

**Solution:** Proper event lifecycle management:
```csharp
private void Start()
{
    if (AuthManager.Instance != null)
    {
        AuthManager.Instance.OnUserChanged += OnUserChanged;
    }
}

private void OnDestroy()
{
    if (AuthManager.Instance != null)
    {
        AuthManager.Instance.OnUserChanged -= OnUserChanged;
    }
}
```

### Challenge 4: Backend Cold Start Issues
**Problem:** Render.com free tier causes backend cold starts, leading to timeout errors during authentication.

**Solution:** Graceful error handling with user feedback:
```csharp
if (request.result != UnityWebRequest.Result.Success)
{
    Debug.LogError($"Error updating statistics: {request.error}");
    ErrorMessageManager.Instance.ShowError("Connection error. Try again.");
}
```

### Challenge 5: Turn Timer State Management
**Problem:** Turn timers needed to persist across scene changes and handle game state transitions properly.

**Solution:** Centralized timer management with state validation:
```csharp
private void StartTurnTimer()
{
    if (turnTimerCoroutine != null)
    {
        StopCoroutine(turnTimerCoroutine); // Cleanup previous timer
    }

    if (isGameActive) // State validation
    {
        currentTurnTimeRemaining = turnTimeLimit;
        turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
    }
}
```

### Turn Timer Anti-Stall System
**Problem:** Players deliberately stalling to frustrate opponents  
**Solution:** 30-second automatic turn switching

```csharp
private IEnumerator TurnTimerCoroutine()
{
    while (currentTurnTimeRemaining > 0 && isGameActive)
    {
        yield return new WaitForSeconds(1f);
        currentTurnTimeRemaining -= 1f;
    }
    
    if (isGameActive && PhotonNetwork.IsMasterClient)
    {
        SwitchToNextTurn();
    }
}
```

## Disconnection Handling & Recovery

### Mid-Game Disconnection Management
**Scenario:** Player disconnects during active gameplay (detected in GameScene)
**Resolution Strategy:**
1. **Automatic Win Assignment:** Remaining player receives victory (+50 points)
2. **Automatic Loss Assignment:** Disconnected player loses points when they reconnect
3. **Seamless Matchmaking Return:** Remaining player returns to matchmaking after 1-second delay

```csharp
public override void OnPlayerLeftRoom(Photon.Realtime.Player otherPlayer)
{
    if (SceneManager.GetActiveScene().name == "MatchmakingMenu")
    {
        PhotonNetwork.LeaveRoom();
    }
    else if (SceneManager.GetActiveScene().name == "GameScene")
    {
        HandlePlayerDisconnectedDuringGame();
    }
}

private void HandlePlayerDisconnectedDuringGame()
{
    if (AuthManager.Instance != null)
    {
        AuthManager.Instance.SendGameResult("win", 50);
    }
    StartCoroutine(ReturnToMatchmakingAfterDisconnect());
}
```

### Connection State Management
- **Automatic reconnection** via PhotonNetwork.ConnectUsingSettings()
- **Session persistence** through encrypted local storage with XOR cipher
- **Token validation** on startup with automatic backend verification
- **State synchronization** via Photon's Master Client authority system

## Performance Optimizations and Debug Removal

### Challenge 6: Debug Log Performance Impact
**Problem:** Extensive debug logging was impacting game performance, especially in production builds.

**Solution:** Systematic removal of non-essential debug logs while preserving critical error logging:
```csharp
// Removed performance-impacting logs:
// Debug.Log($"Checking duplicates for UserId: {myUserId}");
// Debug.Log($"Player {player.NickName} joined. Players: {count}");

// Kept essential error logs:
Debug.LogError("AuthToken or UserId is null! Cannot send game result.");
Debug.LogError($"Disconnected from Photon: {cause}");
```

**Impact:** Reduced I/O operations and string formatting overhead in production builds.

### Challenge 7: Coroutine Management and Cleanup
**Problem:** Multiple coroutines running simultaneously without proper cleanup, causing performance degradation.

**Solution:** Centralized coroutine lifecycle management:
```csharp
private void StartTurnTimer()
{
    if (turnTimerCoroutine != null)
    {
        StopCoroutine(turnTimerCoroutine); // Always cleanup before starting new
    }
    turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine());
}

public override void OnLeftRoom()
{
    isGameActive = false;
    if (turnTimerCoroutine != null)
    {
        StopCoroutine(turnTimerCoroutine);
        turnTimerCoroutine = null; // Clear reference
    }
}
```

### Challenge 8: Unnecessary Network Traffic
**Problem:** Frequent RPC calls and property updates were consuming bandwidth unnecessarily.

**Solution:** Optimized network communication patterns:
```csharp
// Only essential RPCs maintained:
// - SyncTurnRpc: Only when turns actually change
// - RequestMoveRpc: Only for valid moves
// - UpdateOwnerStateRpc: Only when piece ownership changes
```

## Performance Optimizations

### Network Efficiency
- **Minimal RPC usage** - SyncTurnRpc, ExecuteMoveRpc, UpdateOwnerStateRpc only
- **Custom properties** - GameUserId and Username for player identification
- **Event-driven updates** - UI updates only via OnStatsUpdated and OnUserChanged events
- **Optimized data transmission** - PhotonView synchronization for game pieces only

### Memory Management
- **Coroutine management** - Turn timer coroutines with proper StopCoroutine cleanup
- **Event unsubscription** - OnDestroy methods unsubscribe from AuthManager events
- **Singleton pattern** - DontDestroyOnLoad for AuthManager and SimplePunLauncher
- **PhotonView ownership** - Automatic cleanup via PhotonNetwork.Destroy

## Security Considerations

### Data Protection
- **JWT token authentication** with bcryptjs password hashing on backend
- **XOR cipher encryption** for local session data storage
- **Backend API validation** for all authentication and statistics operations
- **MongoDB secure connection** with proper authentication

### Network Security
- **Photon Cloud infrastructure** provides built-in DDoS protection
- **Master Client authority** - only MasterClient can validate moves via RequestMoveRpc
- **Turn validation** - moves only accepted from current turn player
- **Custom properties protection** - GameUserId validation prevents account spoofing

## User Interface Architecture

### UI Components (Assets/Scripts/UI/)
- **TurnTimerUI** - Real-time turn timer
- **UserInfoUI** - Displays user statistics updated via OnStatsUpdated event
- **ErrorMessageManager** - Centralized error message display system
- **LoginButton/RegisterButton** - Authentication UI with backend integration
- **LogOutButton** - Session termination with local data cleanup

## Backend Development and Database Integration

### Problem 6: Building a Secure Authentication Backend
**Challenge:** Creating a robust backend API to handle user authentication, session management, and game statistics without prior Node.js experience.

**Requirements:**
- Secure user registration and login system
- JWT token-based authentication
- Password hashing and validation
- Game statistics tracking and updates
- CORS handling for Unity WebGL builds
- Production deployment on free hosting

**Solution Implemented:**

#### 1. Express.js Server Architecture
```javascript
const express = require('express');
const mongoose = require('mongoose');
const bcryptjs = require('bcryptjs');
const jwt = require('jsonwebtoken');
const cors = require('cors');
const helmet = require('helmet');
const rateLimit = require('express-rate-limit');

const app = express();

// Security middleware
app.use(helmet());
app.use(cors());
app.use(express.json());

// Rate limiting to prevent abuse
const limiter = rateLimit({
  windowMs: 15 * 60 * 1000, // 15 minutes
  max: 100 // limit each IP to 100 requests per windowMs
});
app.use('/api/', limiter);
```

#### 2. MongoDB User Schema Design
```javascript
const userSchema = new mongoose.Schema({
  username: {
    type: String,
    required: true,
    unique: true,
    trim: true,
    minlength: 3,
    maxlength: 20
  },
  password: {
    type: String,
    required: true,
    minlength: 6
  },
  globalScore: {
    type: Number,
    default: 0
  },
  gamesPlayed: {
    type: Number,
    default: 0
  },
  gamesWon: {
    type: Number,
    default: 0
  }
}, { timestamps: true });
```

#### 3. Secure Password Hashing Implementation
```javascript
// Registration endpoint with bcrypt hashing
app.post('/api/auth/register', async (req, res) => {
  try {
    const { username, password } = req.body;

    // Check if user already exists
    const existingUser = await User.findOne({ username });
    if (existingUser) {
      return res.status(400).json({ error: 'Username already exists' });
    }

    // Hash password with salt rounds
    const saltRounds = 12;
    const hashedPassword = await bcryptjs.hash(password, saltRounds);

    // Create new user
    const user = new User({
      username,
      password: hashedPassword
    });

    await user.save();

    // Generate JWT token
    const token = jwt.sign(
      { userId: user._id, username: user.username },
      process.env.JWT_SECRET,
      { expiresIn: '7d' }
    );

    res.status(201).json({
      token,
      userId: user._id,
      username: user.username,
      globalScore: user.globalScore,
      gamesPlayed: user.gamesPlayed,
      gamesWon: user.gamesWon
    });
  } catch (error) {
    res.status(500).json({ error: 'Server error during registration' });
  }
});
```

#### 4. JWT Authentication Middleware
```javascript
const authenticateToken = (req, res, next) => {
  const authHeader = req.headers['authorization'];
  const token = authHeader && authHeader.split(' ')[1]; // Bearer TOKEN

  if (!token) {
    return res.status(401).json({ error: 'Access token required' });
  }

  jwt.verify(token, process.env.JWT_SECRET, (err, user) => {
    if (err) {
      return res.status(403).json({ error: 'Invalid or expired token' });
    }
    req.user = user;
    next();
  });
};
```

#### 5. Game Statistics Update System
```javascript
app.put('/api/auth/stats', authenticateToken, async (req, res) => {
  try {
    const { result, score } = req.body;
    const userId = req.user.userId;

    const user = await User.findById(userId);
    if (!user) {
      return res.status(404).json({ error: 'User not found' });
    }

    // Update statistics based on game result
    user.globalScore += score;
    user.gamesPlayed += 1;

    if (result === 'win') {
      user.gamesWon += 1;
    }

    await user.save();

    res.json({
      globalScore: user.globalScore,
      gamesPlayed: user.gamesPlayed,
      gamesWon: user.gamesWon
    });
  } catch (error) {
    res.status(500).json({ error: 'Error updating statistics' });
  }
});
```

### Backend Technology Stack
- **Node.js/Express** server with CORS and Helmet security
- **MongoDB Atlas** database with Mongoose ODM
- **JWT authentication** with bcryptjs password hashing
- **Express rate limiting** for API protection
- **Hosted on Render.com** with automatic deployments

### API Endpoints (tessellaction-backend.onrender.com)
- **POST /api/auth/login** - User authentication with JWT token generation
- **POST /api/auth/register** - New user registration with password hashing
- **GET /api/auth/me** - User profile retrieval with token validation
- **PUT /api/auth/stats** - Game statistics update (win/loss, score changes)
- **POST /api/auth/verify** - Token validation for session management

### Data Models
```json
{
  "userId": "ObjectId",
  "username": "string",
  "globalScore": "number",
  "gamesPlayed": "number",
  "gamesWon": "number",
  "password": "bcrypt_hash"
}
```

## Testing & Quality Assurance

### Implemented Testing Scenarios
1. **Duplicate account prevention** - Same UserId detection and room ejection
2. **Mid-game disconnection** - Automatic win/loss assignment and matchmaking return
3. **Turn timer functionality** - 30-second automatic turn switching
4. **Backend integration** - Login, register, and statistics update operations
5. **Photon room management** - Join, leave, and property synchronization

### Real Performance Characteristics
- **Matchmaking time:** Depends on player availability and Photon region
- **Turn response latency:** Photon PUN2 typical latency (50-150ms)
- **Memory usage:** Unity standard with PhotonView synchronization overhead
- **Network bandwidth:** RPC calls and PhotonView data only

## Current Deployment Architecture

### Production Environment
- **Unity 2022.3 LTS** for game development
- **Photon PUN2** for real-time networking
- **Render.com** hosting Node.js/Express backend
- **MongoDB Atlas** cloud database
- **GitHub** for version control and deployment

### Current Limitations
- **Single region deployment** - backend hosted on Render.com single instance
- **No CDN** - direct API calls to backend
- **Basic error handling** - limited retry mechanisms
- **Manual scaling** - no auto-scaling implemented

## Actual Implementation Status

### Completed Features
1. **User authentication** with JWT and bcrypt
2. **Real-time matchmaking** via Photon PUN2
3. **Turn-based gameplay** with 30-second timer
4. **Anti-cheat duplicate prevention** system
5. **Disconnection handling** with automatic win/loss
6. **Statistics tracking** with backend integration
7. **UI system** with timer and user info display

### Known Issues
- **Backend cold starts** on Render.com free tier
- **No reconnection recovery** - players must restart game
- **Limited error messages** - basic error handling only
- **No offline mode** - requires internet connection

## Development Lessons and Iterative Improvements

### Lesson 1: Network State Synchronization Complexity
**Initial Approach:** Assumed Photon properties would be immediately available across all clients due to the game simplicity.

**Reality:** Network synchronization has inherent delays and race conditions.

**Adaptation:** Implemented delayed validation patterns and state verification before critical operations.

### Lesson 2: Singleton Pattern in Multiplayer Context
**Challenge:** Managing singleton lifecycle across scene changes while maintaining network connections.

**Solution:** DontDestroyOnLoad pattern with proper cleanup and event management:
```csharp
private void Awake()
{
    if (Instance != null && Instance != this)
    {
        Destroy(gameObject);
        return;
    }
    Instance = this;
    DontDestroyOnLoad(gameObject);
}
```

### Lesson 3: Error Handling in Distributed Systems
**Initial Approach:** Basic try-catch blocks with generic error messages.

**Evolution:** Specific error handling for different failure scenarios:
- Network timeouts vs authentication failures
- Backend unavailability vs invalid credentials

### Lesson 4: User Experience in Multiplayer Games
**Key Insight:** Players need immediate feedback for all actions, especially in networked environments.

**Implementation:**
- Visual feedback for turn timer with color coding
- Immediate error messages for failed operations
- Clear indication of game state (whose turn, connection status)
- Automatic handling of edge cases (disconnections, timeouts)

### Lesson 5: Security vs Performance Trade-offs
**Balance Achieved:**
- Real-time duplicate detection without excessive validation overhead
- Master Client authority without blocking responsive gameplay
- Encrypted local storage without complex key management
- Backend validation without sacrificing user experience

## Conclusion

TessellAction-Duel successfully implements a functional multiplayer turn-based strategy game that solves several critical problems inherent in networked multiplayer gaming. The project demonstrates practical, real-world solutions to common challenges that plague multiplayer game development.

### Problems Successfully Solved:
1. **Account Exploitation Prevention** - Eliminated point farming through same-account detection
2. **Game Stalling Issues** - Resolved through automated 30-second turn timers
3. **Disconnection Chaos** - Automated win/loss assignment with seamless matchmaking return
4. **Session Management Friction** - Persistent authentication with encrypted local storage
5. **Network Synchronization Issues** - Event-driven property updates with proper timing
6. **Performance Degradation** - Systematic debug log removal and coroutine management
7. **Memory Leaks** - Proper event lifecycle management in singleton architecture

### Technical Achievements:
- **Robust authentication system** with JWT, bcrypt, and XOR encryption
- **Real-time anti-cheat system** preventing account exploitation
- **Reliable turn management** with Master Client authority and automatic switching
- **Graceful disconnection handling** maintaining game integrity
- **Event-driven architecture** ensuring UI consistency across network state changes
- **Performance optimization** through selective logging and efficient network patterns

### References
- Unity Photon PUN 2: https://www.youtube.com/playlist?list=PL0iUgXtqnG2gPaXE1hHYoBjqTSWTNwR-6
- Photon PUN 2 Documentation: https://doc.photonengine.com/pun/current/getting-started/pun-intro

- Javascript RESTful APIs: https://www.youtube.com/watch?v=-MTSQjw5DrM
- Javascript backend + MongoDB: https://www.youtube.com/playlist?list=PLbtI3_MArDOkXRLxdMt1NOMtCS-84ibHH

(I had previously worked with Node.js and MongoDB in discord.js projects)
