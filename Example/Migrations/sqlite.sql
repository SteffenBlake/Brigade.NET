--liquibase formatted sql
--changeset brigade:sqlite-schema
CREATE TABLE accounts (id INTEGER PRIMARY KEY, name TEXT NOT NULL, note TEXT NULL);
CREATE TABLE orders (id INTEGER PRIMARY KEY, buyer_id INTEGER NOT NULL, seller_id INTEGER NOT NULL, "order" TEXT NOT NULL, amount INTEGER NOT NULL);
CREATE TABLE order_tags (order_id INTEGER NOT NULL, tag TEXT NOT NULL, PRIMARY KEY (order_id, tag));
--changeset brigade:sqlite-seed
INSERT INTO accounts (id, name, note) VALUES (1, 'O''Reilly', NULL), (2, 'Ada', 'lead'), (3, 'Bob', NULL);
INSERT INTO orders (id, buyer_id, seller_id, "order", amount) VALUES (10, 1, 2, 'alpha', 20), (11, 2, 1, 'beta', 30), (12, 3, 2, 'gamma', 40);
INSERT INTO order_tags (order_id, tag) VALUES (10, 'new'), (10, 'safe'), (11, 'safe'), (12, 'old');
--changeset brigade:sqlite-complex
ALTER TABLE accounts ADD COLUMN parent_id INTEGER NULL;
ALTER TABLE accounts ADD COLUMN "group" TEXT NULL;
ALTER TABLE orders ADD COLUMN category_id INTEGER NOT NULL DEFAULT 101;
ALTER TABLE orders ADD COLUMN status TEXT NOT NULL DEFAULT 'open';
CREATE TABLE categories (id INTEGER PRIMARY KEY, parent_id INTEGER NULL, label TEXT NOT NULL);
CREATE TABLE shipments (id INTEGER PRIMARY KEY, order_id INTEGER NOT NULL, delivered_at TEXT NULL);
UPDATE accounts SET "group" = 'core' WHERE id IN (1, 2);
UPDATE accounts SET "group" = 'edge' WHERE id = 3;
UPDATE accounts SET parent_id = 1 WHERE id IN (2, 3);
UPDATE orders SET category_id = 102 WHERE id = 11;
UPDATE orders SET category_id = 103, status = 'closed' WHERE id = 12;
INSERT INTO accounts (id, name, note, parent_id, "group") VALUES (4, 'Eve', NULL, 2, 'ops'), (5, 'NoOrders', NULL, NULL, 'idle');
INSERT INTO categories (id, parent_id, label) VALUES (100, NULL, 'root'), (101, 100, 'child'), (102, 101, 'grandchild'), (103, 100, 'sibling');
INSERT INTO orders (id, buyer_id, seller_id, "order", amount, category_id, status) VALUES (13, 4, 3, 'delta', 50, 102, 'open'), (14, 2, 4, 'O''Reilly''s deal', 60, 101, 'open');
INSERT INTO order_tags (order_id, tag) VALUES (13, 'safe'), (14, 'safe'), (14, 'quote');
INSERT INTO shipments (id, order_id, delivered_at) VALUES (1000, 10, NULL), (1001, 11, '2026-09-24');
