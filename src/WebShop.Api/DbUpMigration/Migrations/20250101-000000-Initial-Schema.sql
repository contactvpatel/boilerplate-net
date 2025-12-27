/*
 * Initial Schema Migration Script
 * Description: Creates the initial database schema for webshop database
 * 
 * This migration creates all tables in the webshop schema based on the current database structure.
 * It uses IF NOT EXISTS patterns to be idempotent and safe to run on existing databases.
 */

-- Create webshop schema if it doesn't exist
CREATE SCHEMA IF NOT EXISTS webshop;

-- Create ENUM types if they don't exist
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'gender') THEN
        CREATE TYPE public.gender AS ENUM ('male', 'female', 'unisex');
    END IF;
    
    IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'category') THEN
        CREATE TYPE public.category AS ENUM (
            'Apparel',
            'Footwear',
            'Sportswear',
            'Traditional',
            'Formal Wear',
            'Accessories',
            'Watches & Jewelry',
            'Luggage',
            'Cosmetics'
        );
    END IF;
END $$;

-- Set search path to webshop schema
SET search_path TO webshop;

/*
 * Sequences
 */

-- Create sequences if they don't exist
CREATE SEQUENCE IF NOT EXISTS webshop.address_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.articles_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.colors_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.customer_id_seq1
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.labels_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.order_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.order_positions_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.products_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.sizes_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

CREATE SEQUENCE IF NOT EXISTS webshop.stock_id_seq
    AS integer
    START WITH 1
    INCREMENT BY 1
    NO MINVALUE
    NO MAXVALUE
    CACHE 1;

/*
 * Lookup Tables (With audit fields)
 */

-- Labels/Brands table
CREATE TABLE IF NOT EXISTS webshop.labels (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.labels_id_seq'::regclass),
    name TEXT,
    slugname TEXT,
    icon BYTEA,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Colors table
CREATE TABLE IF NOT EXISTS webshop.colors (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.colors_id_seq'::regclass),
    name TEXT,
    rgb TEXT,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Sizes table
CREATE TABLE IF NOT EXISTS webshop.sizes (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.sizes_id_seq'::regclass),
    gender public.gender,
    category public.category,
    size TEXT,
    size_us INT4RANGE,
    size_uk INT4RANGE,
    size_eu INT4RANGE,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

/*
 * Core Tables (With audit fields)
 */

-- Customer table
CREATE TABLE IF NOT EXISTS webshop.customer (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.customer_id_seq1'::regclass),
    firstname TEXT,
    lastname TEXT,
    gender public.gender,
    email TEXT,
    dateofbirth DATE,
    currentaddressid INTEGER,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Address table
CREATE TABLE IF NOT EXISTS webshop.address (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.address_id_seq'::regclass),
    customerid INTEGER,
    firstname TEXT,
    lastname TEXT,
    address1 TEXT,
    address2 TEXT,
    city TEXT,
    zip TEXT,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Products table
CREATE TABLE IF NOT EXISTS webshop.products (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.products_id_seq'::regclass),
    name TEXT,
    labelid INTEGER,
    category public.category,
    gender public.gender,
    currentlyactive BOOLEAN,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Articles table
CREATE TABLE IF NOT EXISTS webshop.articles (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.articles_id_seq'::regclass),
    productid INTEGER,
    ean TEXT,
    colorid INTEGER,
    size INTEGER,
    description TEXT,
    originalprice MONEY,
    reducedprice MONEY,
    taxrate NUMERIC,
    discountinpercent INTEGER,
    currentlyactive BOOLEAN,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Order table
CREATE TABLE IF NOT EXISTS webshop."order" (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.order_id_seq'::regclass),
    customer INTEGER,
    ordertimestamp TIMESTAMP WITH TIME ZONE DEFAULT now(),
    shippingaddressid INTEGER,
    total MONEY,
    shippingcost MONEY,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Order positions table
CREATE TABLE IF NOT EXISTS webshop.order_positions (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.order_positions_id_seq'::regclass),
    orderid INTEGER,
    articleid INTEGER,
    amount SMALLINT,
    price MONEY,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

-- Stock table
CREATE TABLE IF NOT EXISTS webshop.stock (
    id INTEGER NOT NULL PRIMARY KEY DEFAULT nextval('webshop.stock_id_seq'::regclass),
    articleid INTEGER,
    count INTEGER,
    isactive BOOLEAN NOT NULL DEFAULT true,
    createdby INTEGER NOT NULL,
    created TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT now(),
    updatedby INTEGER NOT NULL,
    updated TIMESTAMP WITH TIME ZONE
);

/*
 * Foreign Key Constraints
 */

-- Address foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_address_customer' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.address 
        ADD CONSTRAINT FK_address_customer 
        FOREIGN KEY (customerid) REFERENCES webshop.customer(id);
    END IF;
END $$;

-- Product foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_products_label' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.products 
        ADD CONSTRAINT FK_products_label 
        FOREIGN KEY (labelid) REFERENCES webshop.labels(id);
    END IF;
END $$;

-- Article foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_articles_product' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.articles 
        ADD CONSTRAINT FK_articles_product 
        FOREIGN KEY (productid) REFERENCES webshop.products(id);
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_articles_color' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.articles 
        ADD CONSTRAINT FK_articles_color 
        FOREIGN KEY (colorid) REFERENCES webshop.colors(id);
    END IF;
END $$;

-- Order foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_order_customer' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop."order" 
        ADD CONSTRAINT FK_order_customer 
        FOREIGN KEY (customer) REFERENCES webshop.customer(id);
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_order_address' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop."order" 
        ADD CONSTRAINT FK_order_address 
        FOREIGN KEY (shippingaddressid) REFERENCES webshop.address(id);
    END IF;
END $$;

-- Order position foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_order_positions_order' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.order_positions 
        ADD CONSTRAINT FK_order_positions_order 
        FOREIGN KEY (orderid) REFERENCES webshop."order"(id);
    END IF;
    
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_order_positions_article' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.order_positions 
        ADD CONSTRAINT FK_order_positions_article 
        FOREIGN KEY (articleid) REFERENCES webshop.articles(id);
    END IF;
END $$;

-- Stock foreign keys
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_stock_article' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.stock 
        ADD CONSTRAINT FK_stock_article 
        FOREIGN KEY (articleid) REFERENCES webshop.articles(id);
    END IF;
END $$;

-- Customer current address foreign key
DO $$
BEGIN
    IF NOT EXISTS (
        SELECT 1 FROM pg_constraint 
        WHERE conname = 'FK_customer_address' 
        AND connamespace = (SELECT oid FROM pg_namespace WHERE nspname = 'webshop')
    ) THEN
        ALTER TABLE webshop.customer 
        ADD CONSTRAINT FK_customer_address 
        FOREIGN KEY (currentaddressid) REFERENCES webshop.address(id);
    END IF;
END $$;

/*
 * Indexes for better query performance
 */

-- Customer indexes
CREATE INDEX IF NOT EXISTS idx_customer_email ON webshop.customer(email) WHERE isactive = TRUE;
CREATE INDEX IF NOT EXISTS idx_customer_currentaddressid ON webshop.customer(currentaddressid);

-- Address indexes
CREATE INDEX IF NOT EXISTS idx_address_customerid ON webshop.address(customerid) WHERE isactive = TRUE;

-- Product indexes
CREATE INDEX IF NOT EXISTS idx_products_labelid ON webshop.products(labelid);
CREATE INDEX IF NOT EXISTS idx_products_category ON webshop.products(category);
CREATE INDEX IF NOT EXISTS idx_products_currentlyactive ON webshop.products(currentlyactive) WHERE currentlyactive = TRUE AND isactive = TRUE;

-- Article indexes
CREATE INDEX IF NOT EXISTS idx_articles_productid ON webshop.articles(productid);
CREATE INDEX IF NOT EXISTS idx_articles_colorid ON webshop.articles(colorid);
CREATE INDEX IF NOT EXISTS idx_articles_ean ON webshop.articles(ean);
CREATE INDEX IF NOT EXISTS idx_articles_currentlyactive ON webshop.articles(currentlyactive) WHERE currentlyactive = TRUE AND isactive = TRUE;

-- Order indexes
CREATE INDEX IF NOT EXISTS idx_order_customer ON webshop."order"(customer);
CREATE INDEX IF NOT EXISTS idx_order_ordertimestamp ON webshop."order"(ordertimestamp);
CREATE INDEX IF NOT EXISTS idx_order_shippingaddressid ON webshop."order"(shippingaddressid);

-- Order position indexes
CREATE INDEX IF NOT EXISTS idx_order_positions_orderid ON webshop.order_positions(orderid);
CREATE INDEX IF NOT EXISTS idx_order_positions_articleid ON webshop.order_positions(articleid);

-- Stock indexes
CREATE INDEX IF NOT EXISTS idx_stock_articleid ON webshop.stock(articleid);
CREATE INDEX IF NOT EXISTS idx_stock_articleid_active ON webshop.stock(articleid) WHERE isactive = TRUE;

-- Reset search path
RESET search_path;

