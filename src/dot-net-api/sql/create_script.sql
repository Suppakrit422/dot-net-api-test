CREATE TABLE queue_counter (
    id         smallint PRIMARY KEY DEFAULT 1 CHECK (id = 1),  -- forces a single row
    cycle_no   int         NOT NULL DEFAULT 1,
    last_seq   int         NOT NULL DEFAULT -1 CHECK (last_seq BETWEEN -1 AND 259),
    updated_at timestamptz NOT NULL DEFAULT now()
);


CREATE TABLE queue_ticket (
    id        bigserial PRIMARY KEY,
    cycle_no  int         NOT NULL,
    seq       int         NOT NULL CHECK (seq BETWEEN 0 AND 259),
    queue_no  varchar(2)  NOT NULL,          -- 'A0'..'Z9'
    issued_at timestamptz NOT NULL DEFAULT now(),
    UNIQUE (cycle_no, seq)
);