--liquibase formatted sql
--changeset brigade:mariadb-schema
CREATE TABLE accounts (id INT PRIMARY KEY, name VARCHAR(100) NOT NULL, note VARCHAR(100) NULL);
CREATE TABLE purchases (id INT PRIMARY KEY, buyer_id INT NOT NULL, seller_id INT NOT NULL, `purchase` VARCHAR(100) NOT NULL, amount INT NOT NULL);
CREATE TABLE purchase_tags (purchase_id INT NOT NULL, tag VARCHAR(40) NOT NULL, PRIMARY KEY (purchase_id, tag));
--changeset brigade:mariadb-seed
INSERT INTO accounts (id, name, note) VALUES (1, 'O''Reilly', NULL), (2, 'Ada', 'lead'), (3, 'Bob', NULL);
INSERT INTO purchases (id, buyer_id, seller_id, `purchase`, amount) VALUES (10, 1, 2, 'alpha', 20), (11, 2, 1, 'beta', 30), (12, 3, 2, 'gamma', 40);
INSERT INTO purchase_tags (purchase_id, tag) VALUES (10, 'new'), (10, 'safe'), (11, 'safe'), (12, 'old');
--changeset brigade:mariadb-complex
ALTER TABLE accounts ADD COLUMN parent_id INT NULL;
ALTER TABLE accounts ADD COLUMN `group` VARCHAR(40) NULL;
ALTER TABLE purchases ADD COLUMN category_id INT NOT NULL DEFAULT 101;
ALTER TABLE purchases ADD COLUMN status VARCHAR(20) NOT NULL DEFAULT 'open';
CREATE TABLE categories (id INT PRIMARY KEY, parent_id INT NULL, label VARCHAR(100) NOT NULL);
CREATE TABLE shipments (id INT PRIMARY KEY, purchase_id INT NOT NULL, delivered_at VARCHAR(30) NULL);
UPDATE accounts SET `group` = 'core' WHERE id IN (1, 2);
UPDATE accounts SET `group` = 'edge' WHERE id = 3;
UPDATE accounts SET parent_id = 1 WHERE id IN (2, 3);
UPDATE purchases SET category_id = 102 WHERE id = 11;
UPDATE purchases SET category_id = 103, status = 'closed' WHERE id = 12;
INSERT INTO accounts (id, name, note, parent_id, `group`) VALUES (4, 'Eve', NULL, 2, 'ops'), (5, 'NoPurchases', NULL, NULL, 'idle');
INSERT INTO categories (id, parent_id, label) VALUES (100, NULL, 'root'), (101, 100, 'child'), (102, 101, 'grandchild'), (103, 100, 'sibling');
INSERT INTO purchases (id, buyer_id, seller_id, `purchase`, amount, category_id, status) VALUES (13, 4, 3, 'delta', 50, 102, 'open'), (14, 2, 4, 'O''Reilly''s deal', 60, 101, 'open');
INSERT INTO purchase_tags (purchase_id, tag) VALUES (13, 'safe'), (14, 'safe'), (14, 'quote');
INSERT INTO shipments (id, purchase_id, delivered_at) VALUES (1000, 10, NULL), (1001, 11, '2026-09-24');
