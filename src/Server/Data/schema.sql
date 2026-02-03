-- SuperDeck Database Schema (SQLite Compatible)

CREATE TABLE IF NOT EXISTS Characters (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Level INTEGER DEFAULT 1,
    XP INTEGER DEFAULT 0,
    Attack INTEGER DEFAULT 0,
    Defense INTEGER DEFAULT 0,
    Speed INTEGER DEFAULT 5,
    DeckCardIds TEXT,
    Wins INTEGER DEFAULT 0,
    Losses INTEGER DEFAULT 0,
    MMR INTEGER DEFAULT 1000,
    IsGhost INTEGER DEFAULT 0,
    IsPublished INTEGER DEFAULT 0,
    OwnerPlayerId TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    LastModified TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS GhostSnapshots (
    Id TEXT PRIMARY KEY,
    SourceCharacterId TEXT NOT NULL,
    SerializedCharacterState TEXT NOT NULL,
    GhostMMR INTEGER DEFAULT 1000,
    Wins INTEGER DEFAULT 0,
    Losses INTEGER DEFAULT 0,
    TimesUsed INTEGER DEFAULT 0,
    AIProfileId TEXT NOT NULL,
    DownloadedAt TEXT,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS AIProfiles (
    Id TEXT PRIMARY KEY,
    Name TEXT NOT NULL,
    Description TEXT,
    BehaviorRules TEXT NOT NULL,
    Difficulty INTEGER,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Indexes for performance
CREATE INDEX IF NOT EXISTS idx_characters_mmr ON Characters(MMR);
CREATE INDEX IF NOT EXISTS idx_characters_owner ON Characters(OwnerPlayerId);
CREATE INDEX IF NOT EXISTS idx_characters_ghost ON Characters(IsGhost);
CREATE INDEX IF NOT EXISTS idx_ghosts_mmr ON GhostSnapshots(GhostMMR);
CREATE INDEX IF NOT EXISTS idx_ghosts_source ON GhostSnapshots(SourceCharacterId);

-- Default AI Profile
INSERT OR IGNORE INTO AIProfiles (Id, Name, Description, BehaviorRules, Difficulty)
VALUES (
    'default',
    'Default AI',
    'Basic AI that prioritizes attacks',
    '{"priorityAttackWhenHPadvantage": 0.7, "alwaysQueueMaxCards": true, "defensiveThreshold": 0.3}',
    5
);
