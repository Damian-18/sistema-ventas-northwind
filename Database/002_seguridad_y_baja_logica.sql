SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND column_name = 'failed_login_attempts'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `api_users` ADD COLUMN `failed_login_attempts` INT NOT NULL DEFAULT 0',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND column_name = 'lockout_end'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `api_users` ADD COLUMN `lockout_end` DATETIME NULL',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND column_name = 'customer_id'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `api_users` ADD COLUMN `customer_id` CHAR(5) NULL',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND column_name = 'employee_id'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `api_users` ADD COLUMN `employee_id` INT NULL',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @index_exists = (
    SELECT COUNT(*)
    FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND index_name = 'UX_api_users_customer_id'
);
SET @ddl = IF(
    @index_exists = 0,
    'ALTER TABLE `api_users` ADD UNIQUE INDEX `UX_api_users_customer_id` (`customer_id`)',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @index_exists = (
    SELECT COUNT(*)
    FROM information_schema.statistics
    WHERE table_schema = DATABASE()
      AND table_name = 'api_users'
      AND index_name = 'UX_api_users_employee_id'
);
SET @ddl = IF(
    @index_exists = 0,
    'ALTER TABLE `api_users` ADD UNIQUE INDEX `UX_api_users_employee_id` (`employee_id`)',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'customers'
      AND column_name = 'is_active'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `customers` ADD COLUMN `is_active` TINYINT(1) NOT NULL DEFAULT 1',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;

SET @column_exists = (
    SELECT COUNT(*)
    FROM information_schema.columns
    WHERE table_schema = DATABASE()
      AND table_name = 'employees'
      AND column_name = 'is_active'
);
SET @ddl = IF(
    @column_exists = 0,
    'ALTER TABLE `employees` ADD COLUMN `is_active` TINYINT(1) NOT NULL DEFAULT 1',
    'SELECT 1'
);
PREPARE migration_statement FROM @ddl;
EXECUTE migration_statement;
DEALLOCATE PREPARE migration_statement;
