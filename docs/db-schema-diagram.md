# Database Schema Diagram

This document contains a Mermaid Entity Relationship Diagram (ERD) for the WebShop database schema.

## Entity Relationship Diagram

```mermaid
erDiagram
    %% Lookup Tables
    Labels {
        int id PK
        text name
        text slugname
        bytea icon
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Colors {
        int id PK
        text name
        text rgb
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Sizes {
        int id PK
        enum gender
        enum category
        text size
        int4range size_us
        int4range size_uk
        int4range size_eu
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    %% Core Tables
    Customer {
        int id PK
        text firstname
        text lastname
        enum gender
        text email
        date dateofbirth
        int currentaddressid FK
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Address {
        int id PK
        int customerid FK
        text firstname
        text lastname
        text address1
        text address2
        text city
        text zip
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Products {
        int id PK
        text name
        int labelid FK
        enum category
        enum gender
        boolean currentlyactive
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Articles {
        int id PK
        int productid FK
        text ean
        int colorid FK
        int size
        text description
        money originalprice
        money reducedprice
        numeric taxrate
        int discountinpercent
        boolean currentlyactive
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Order {
        int id PK
        int customer FK
        timestamp ordertimestamp
        int shippingaddressid FK
        money total
        money shippingcost
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    OrderPositions {
        int id PK
        int orderid FK
        int articleid FK
        smallint amount
        money price
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    Stock {
        int id PK
        int articleid FK
        int count
        boolean isactive
        int createdby
        timestamp created
        int updatedby
        timestamp updated
    }

    %% Relationships
    Customer ||--o| Address : "has current address"
    Customer ||--o{ Address : "has addresses"
    Customer ||--o{ Order : "places"

    Address ||--o{ Order : "used as shipping address"

    Labels ||--o{ Products : "branded as"

    Products ||--o{ Articles : "contains"

    Colors ||--o{ Articles : "available in"

    Articles ||--o{ OrderPositions : "ordered as"
    Articles ||--o| Stock : "has stock"

    Order ||--o{ OrderPositions : "contains"
```

## Table Descriptions

### Lookup Tables

- **Labels**: Brand/label information for products
- **Colors**: Color definitions with RGB values
- **Sizes**: Size definitions with US, UK, and EU conversions

### Core Tables

- **Customer**: Customer information with current address reference
- **Address**: Customer addresses (can have multiple per customer)
- **Products**: Product groups that contain multiple articles
- **Articles**: Specific product instances with size, color, and pricing
- **Order**: Order header with customer and shipping information
- **OrderPositions**: Order line items linking orders to articles
- **Stock**: Inventory count for each article

## Key Relationships

1. **Customer → Address**: One-to-many (customer can have multiple addresses)
2. **Customer → Address**: One-to-one (current address reference)
3. **Customer → Order**: One-to-many (customer can place multiple orders)
4. **Address → Order**: One-to-many (address can be used in multiple orders)
5. **Labels → Products**: One-to-many (label can have multiple products)
6. **Products → Articles**: One-to-many (product can have multiple articles)
7. **Colors → Articles**: One-to-many (color can be used in multiple articles)
8. **Articles → OrderPositions**: One-to-many (article can be in multiple orders)
9. **Articles → Stock**: One-to-one (each article has one stock entry)
10. **Order → OrderPositions**: One-to-many (order contains multiple positions)

## Audit Fields

All tables include standard audit fields:
- `isactive`: Soft delete flag (default: true)
- `createdby`: User ID who created the record
- `created`: Timestamp when record was created
- `updatedby`: User ID who last updated the record
- `updated`: Timestamp when record was last updated

## Indexes

The schema includes optimized indexes for:
- Email lookups (Customer)
- Foreign key relationships
- Active record filtering (partial indexes)
- Category and label filtering (Products)
- EAN lookups (Articles)
- Order timestamp queries
- Stock queries by article
