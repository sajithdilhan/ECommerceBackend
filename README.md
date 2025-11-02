# Senior .NET Developer Take-Home Test

## Thought Process

### Main Tasks
1. Build two independant microservices.
2. Implement RESTful APIs for user and order management.
4. Use event-driven communication between services (cross-service communication).
5. Containerize both services.

### Additional Tasks
1. Add service health checks
2. Add logging.
3. Use TDD approach for a Comprehensive test coverage.
4. Add retry, circuit breaker

### Assumptions
1. Should the services have authentication and authorization? => Assumed no for simplicity.
2. What to do with the kafka events => 
    1. Assumed `UserCreated` event is notifying Order service to keep track of userIds for validation. Otherwise order should not be created.
    2. Assumed `OrderCreated` event is notifying User service to keep the order count for a particular user. Can be extended to send notifications via another service.

### Highlevel Technical Design
1. Use ASP.NET Core Web API projects (.NET 8).
2. USe single GitHub repo, single solution with four projects(Not real world style - just to make it easier to clone, run and review the solution).
3. Use EF Core with in-memory database for data storage.
4. Use Confluent.Kafka for events handling.
6. Add a shared class library to keep common functionalities. 
7. Add a unit tests project to cover all three projects(Not real world style).
8. Add Logging to log important events by each services.
8. Use Docker for containarisation with docker-compose.
9. Add Swagger support for APIs.

### Highlevel Functional Design
1. User Service
    - Expose REST endpoints to create and get users.
    - Validate user input.
    - Store user data in an in-memory database.
    - Publish `UserCreated` event to Kafka on user creation.
    - Subscribe to `OrderCreated` events to update the order count for users.
    - Increment orders count per user.
2. Order Service
    - Expose endpoints to create and get orders.
    - Validate order input.
    - Store order data in an in-memory database.
    - Publish `OrderCreated` event to Kafka on order creation.
    - Subscribe to `UserCreated` events to validate user existence before creating an order.
3. Shared Library
    - Define common models for User and Order.
    - Define event models for `UserCreated` and `OrderCreated`.
    - Implement Kafka producer and consumer utilities.


## Setup Instructions

### Prerequisites
- **.NET 8 SDK** 
- **Docker Desktop**
- **Git**

### Steps to Run the Application
1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd IS-Backend_TakeHomeAssessment
   ```
2. Start the services using Docker Compose:
   ```bash
    docker-compose up --build
    ```
3. Access the services:
    - User Service: `http://localhost:5000`
    - Order Service: `http://localhost:5001`


## Resources 
- ChatGPT 
- Youtube

### AI prompts
- Add swagger UI support for .net core web api.
- Add API versioning in .net core web api.

                            
