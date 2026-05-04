-- Data/sql/create_tables.sql

CREATE TABLE IF NOT EXISTS TB_ASSET_MASTER (
    TICKER        TEXT PRIMARY KEY,
    NAME          TEXT NOT NULL,
    CURRENCY      TEXT NOT NULL DEFAULT 'USD',
    IS_ACTIVE     INTEGER NOT NULL DEFAULT 1,
    CREATED_AT    TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS TB_INVEST_STRATEGY (
    STRATEGY_ID   INTEGER PRIMARY KEY AUTOINCREMENT,
    STRATEGY_NAME TEXT NOT NULL,
    TICKER        TEXT NOT NULL,
    WEIGHT        REAL NOT NULL,
    FOREIGN KEY (TICKER) REFERENCES TB_ASSET_MASTER(TICKER)
);

CREATE TABLE IF NOT EXISTS TB_TRADE_HISTORY (
    TRADE_ID      INTEGER PRIMARY KEY AUTOINCREMENT,
    TRADE_DATE    TEXT NOT NULL,
    TICKER        TEXT NOT NULL,
    ORDER_TYPE    TEXT NOT NULL,  -- BUY / SELL
    QTY           INTEGER NOT NULL,
    PRICE         REAL NOT NULL,
    STATUS        TEXT NOT NULL DEFAULT 'PENDING', -- PENDING / FILLED / FAILED
    ORDER_NO      TEXT,
    CREATED_AT    TEXT NOT NULL DEFAULT (datetime('now','localtime'))
);

CREATE TABLE IF NOT EXISTS TB_APP_CONFIG (
    CONFIG_KEY    TEXT PRIMARY KEY,
    CONFIG_VALUE  TEXT NOT NULL,
    DESCRIPTION   TEXT
);

-- 초기 마스터 데이터
INSERT OR IGNORE INTO TB_ASSET_MASTER (TICKER, NAME, CURRENCY) VALUES
    ('SCHD',  'Schwab US Dividend Equity ETF', 'USD'),
    ('QQQM',  'Invesco NASDAQ 100 ETF',        'USD'),
    ('GLD',   'SPDR Gold Shares',              'USD'),
    ('JEPI',  'JPMorgan Equity Premium Income','USD'),
    ('SPLG',  'SPDR Portfolio S&P 500 ETF',    'USD');

-- 초기 전략 데이터 (공격형)
INSERT OR IGNORE INTO TB_INVEST_STRATEGY (STRATEGY_NAME, TICKER, WEIGHT) VALUES
    ('공격형', 'QQQM', 0.50),
    ('공격형', 'SPLG', 0.30),
    ('공격형', 'GLD',  0.20);

-- 초기 전략 데이터 (안정형)
INSERT OR IGNORE INTO TB_INVEST_STRATEGY (STRATEGY_NAME, TICKER, WEIGHT) VALUES
    ('안정형', 'SCHD', 0.40),
    ('안정형', 'JEPI', 0.40),
    ('안정형', 'GLD',  0.20);

-- 앱 기본 설정
INSERT OR IGNORE INTO TB_APP_CONFIG (CONFIG_KEY, CONFIG_VALUE, DESCRIPTION) VALUES
    ('IS_PAPER_TRADING', '1',        '1=모의투자 0=실거래'),
    ('INVEST_AMOUNT_KRW','1000000',  '월 투자금액(원)'),
    ('ACTIVE_STRATEGY',  '안정형',   '현재 활성 전략'),
    ('ORDER_SCHEDULE',   '22:30',    '주문 실행 시각(KST)'),
    ('REBALANCE_ENABLED','0',        '리밸런싱 활성화 여부'),
    ('REBALANCE_PERIOD', 'MONTHLY',  '리밸런싱 주기 (WEEKLY/MONTHLY)'),
    ('REBALANCE_THRESHOLD','0.05',   '리밸런싱 편차 임계값'),
    ('LAST_REBALANCE_DATE','',       '마지막 리밸런싱 실행일');

-- ═══════════════════════════════════════════════════════
-- Phase 2.5: 퀀트 엔진 확장 테이블
-- ═══════════════════════════════════════════════════════

-- TB_INVEST_STRATEGY에 전략 유형 컬럼 추가 (이미 존재하면 무시)
-- SQLite는 ALTER TABLE ADD COLUMN IF NOT EXISTS를 지원하지 않으므로
-- 마이그레이션 시 에러 무시 방식 사용
-- ALTER TABLE TB_INVEST_STRATEGY ADD COLUMN STRATEGY_TYPE TEXT DEFAULT 'MEAN_REVERSION';

-- 매수/매도 시점 시장 지표 스냅샷 (Phase 4 AI 학습 데이터)
CREATE TABLE IF NOT EXISTS TB_MARKET_SNAPSHOT (
    SNAPSHOT_ID     INTEGER PRIMARY KEY AUTOINCREMENT,
    SNAP_DATE       TEXT    NOT NULL,
    TICKER          TEXT    NOT NULL,
    PRICE           REAL,
    POSITION_20D    REAL,
    RSI_14          REAL,
    MACD_VALUE      REAL,
    MACD_SIGNAL     REAL,
    BB_UPPER        REAL,
    BB_LOWER        REAL,
    SIGNAL          TEXT,
    CREATED_AT      TEXT    DEFAULT (DATETIME('now','localtime'))
);