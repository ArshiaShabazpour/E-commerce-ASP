# Online Shop Backend 

**Online Shop Backend** is a  backend API for a e-commerce platform. Built with **ASP.NET Core** and **Entity Framework Core**, it provides endpoints to manage customers, products, orders, payments, carts, feedback, cancellations, refunds, and addresses.

---

## Features

- **Customers:** Registration, login, update profile, change password.  
- **Addresses:** CRUD operations for customer addresses.  
- **Products:** CRUD, get by category, update status.  
- **Categories:** CRUD operations.  
- **Carts:** Add, update, remove, and clear cart items.  
- **Orders:** Create orders, update status, retrieve orders.  
- **Payments:** Process payments, update status, handle COD.  
- **Feedback:** Submit, update, delete, retrieve product feedback.  
- **Cancellations:** Request and manage order cancellations.  
- **Refunds:** Process refunds for canceled orders, manual updates by admin.

---

## API Endpoints

### **Addresses**
- `POST /api/Addresses/CreateAddress`  
- `GET /api/Addresses/GetAddressById/{id}`  
- `PUT /api/Addresses/UpdateAddress`  
- `DELETE /api/Addresses/DeleteAddress`  
- `GET /api/Addresses/GetAddressesByCustomer/{customerId}`  

### **Cancellations**
- `POST /api/Cancellations/RequestCancellation`  
- `GET /api/Cancellations/GetAllCancellations`  
- `GET /api/Cancellations/GetCancellationById/{id}`  
- `PUT /api/Cancellations/UpdateCancellationStatus`  

### **Carts**
- `GET /api/Carts/GetCart/{customerId}`  
- `POST /api/Carts/AddToCart`  
- `PUT /api/Carts/UpdateCartItem`  
- `DELETE /api/Carts/RemoveCartItem`  
- `DELETE /api/Carts/ClearCart`  

### **Categories**
- `POST /api/Categories/CreateCategory`  
- `GET /api/Categories/GetCategoryById/{id}`  
- `PUT /api/Categories/UpdateCategory`  
- `DELETE /api/Categories/DeleteCategory/{id}`  
- `GET /api/Categories/GetAllCategories`  

### **Customers**
- `POST /api/Customers/RegisterCustomer`  
- `POST /api/Customers/Login`  
- `GET /api/Customers/GetCustomerById/{id}`  
- `PUT /api/Customers/UpdateCustomer`  
- `DELETE /api/Customers/DeleteCustomer/{id}`  
- `POST /api/Customers/ChangePassword`  

### **Feedback**
- `POST /api/Feedback/SubmitFeedback`  
- `GET /api/Feedback/GetFeedbackForProduct/{productId}`  
- `GET /api/Feedback/GetAllFeedback`  
- `PUT /api/Feedback/UpdateFeedback`  
- `DELETE /api/Feedback/DeleteFeedback`  

### **Orders**
- `POST /api/Orders/CreateOrder`  
- `GET /api/Orders/GetOrderById/{id}`  
- `PUT /api/Orders/UpdateOrderStatus`  
- `GET /api/Orders/GetAllOrders`  
- `GET /api/Orders/GetOrdersByCustomer/{customerId}`  

### **Payments**
- `POST /api/Payments/ProcessPayment`  
- `GET /api/Payments/GetPaymentById/{paymentId}`  
- `GET /api/Payments/GetPaymentByOrderId/{orderId}`  
- `PUT /api/Payments/UpdatePaymentStatus`  
- `POST /api/Payments/CompleteCODPayment`  

### **Products**
- `POST /api/Products/CreateProduct`  
- `GET /api/Products/GetProductById/{id}`  
- `PUT /api/Products/UpdateProduct`  
- `DELETE /api/Products/DeleteProduct/{id}`  
- `GET /api/Products/GetAllProducts`  
- `GET /api/Products/GetAllProductsByCategory/{categoryId}`  
- `PUT /api/Products/UpdateProductStatus`  

### **Refunds**
- `GET /api/Refunds/GetEligibleRefunds`  
- `POST /api/Refunds/ProcessRefund`  
- `PUT /api/Refunds/UpdateRefundStatus`  
- `GET /api/Refunds/GetRefundById/{id}`  
- `GET /api/Refunds/GetAllRefunds`  

---

## Project Structure

