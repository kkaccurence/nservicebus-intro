# NServiceBus Retail Demo with .NET Core
**With .NET Core, Docker, and RabbitMQ**

This is a sample project that gives you and introduction into to building distributed systems with NServiceBus.  The project is based on Particular's tutorial at https://docs.particular.net/tutorials/intro-to-nservicebus/3-multiple-endpoints but is using NServiceBus version 7, Docker containers, and RabbitMQ as the transport.

An accompanying slide deck that introduces some of the concepts of distributed systems and Service Oriented Architecure is also available at https://www.slideshare.net/ChrisMorgan8/introduction-to-microservices-with-nservice-bus.

Each of the steps or stages below are available as branches or you can get the fully implement demo by checking out the HEAD of master.

### Prerequisites ###
- Windows 10
- Visual Studio 2017 (community edition is fine)
- Docker for Windows with Linux container support enabled
- NServiceBus dotnet new templates

### Start RabbitMQ ###
Before running any containers for the website start the RabbitMQ container.

Open a command or powershell prompt and cChange to the root directory of where you cloned the repository to

Run `docker-compose up -d`

You can now access the RabbitMQ Management console at http://localhost:15672

Username: `retaildemo`

Password: `password`

To stop RabbitMQ run `docker-compose down`


## Step One ##
This step starts you out with a working web site that allows you to checkout and place an order but does not publish any events and there are no endpoints yet.

Run the project in Visual Studio with <kbd>F5</kbd> to launch the store web site at http://localhost:32773/

Click the **proceed to checkout** button to go to the checkout page.  Here you will see that there is an order id that has been generated that will be sent to the **PlaceOrder** command that will be added in step two.

Click **Place your order** to see that the CheckoutController.PlaceOrder() action method is called and the confirmation page is loaded.  In the next step you will publish the **PlaceOrder** command from this action method.


## Step Two ##
This step adds publishing of the **PlaceOrder** command in the checkout controller and handling of the command in a Sales endpoint.

#### Create the Sales Endpoint ###

From a command or bash window

Run `dotnet new -i "ParticularTemplates::*"` to install the NServiceBus dotnet new template
 
From the `\src` directory run the following commands to create a Sales.Endpoints project using the NServiceBus Docker Container template: 

    mkdir Sales.Endpoints    
    cd Sales.Endpoints
	dotnet new nsbdockercontainer

Add the NServiceBus.RabbitMQ package to the Sales.Endpoints project

`Install-Package NServiceBus.RabbitMQ`

In Visual Studio add the new project to the Sales solution folder

Edit the Hosts.cs file and change the following:

- Change `EndpointName` to `sales`

- Edit the `EndpointConfiguration` object's setting to match those in the store-web

Add a `Sales.Endpoints.PlaceOrderHandler` to handle the `PlaceOrder` command

Add a `shipping` service to the `/src/docker-compose.yaml` file


 
## Step Three ##
This steps adds publishing of the`OrderPlaced` event in the `PlaceOrderHandler` and handling it using the Billing endpoint.

From the `\src` directory run the following commands to create a Billing.Endpoints project using the NServiceBus Docker Container template: 

    mkdir Billing.Endpoints    
    cd Billing.Endpoints
	dotnet new nsbdockercontainer

Add the NServiceBus.RabbitMQ package to the Billing.Endpoints project

`Install-Package NServiceBus.RabbitMQ`

In Visual Studio add the created project to the Billing solution folder.
Edit the Hosts.cs file
Change `EndpointName` to `billing`

Edit the EndpointConfiguration settings to use Infrastructure.Endpoint.

Create an `OrderPlaced` event in `Sales.Messages.Events`

Publish the `OrderPlaced` event from the `Sales.Endpoints.PlaceOrderHandler` handler

Add a `Billing.Endpoints.OrderPlacedHandler` to handle the `OrderPlaced` event

Add a `billing` service to the `/src/docker-compose.yaml` file


## Step Four ##
This steps adds handling of the `OrderPlaced` event in the Shipping endpoint.

From the `\src` directory run the following commands to create a Shipping.Endpoints project using the NServiceBus Docker Container template: 

    mkdir Shipping.Endpoints    
    cd Shipping.Endpoints
	dotnet new nsbdockercontainer

Add the NServiceBus.RabbitMQ package to the Billing.Endpoints project

`Install-Package NServiceBus.RabbitMQ`

In Visual Studio add the created project to the Billing solution folder.
Edit the Hosts.cs file
Change `EndpointName` to `shipping`
Edit the EndpointConfiguration settings to use Infrastructure.Endpoint.

Add a `Shipping.Endpoints.OrderPlacedHandler` to handle the `OrderPlaced` event

Add a `shipping` service to the `/src/docker-compose.yaml` file


## Step Five ##
This steps adds publishing of `OrderBilled` event and handling of it in the Shipping endpoint.

In the `Billing.Endpoints.OrderPlacedHandler` publish an `OrderBilled` event.
Create a Billing.Messages project with an Events directly that contains the `OrderBilled` event.  This event just needs an `OrderId` property. 

Add a `Shipping.Endpoints.OrderBilledHandler` to handle the `OrderBilled` event

At this point the system publishes the OrderPlaced event which is handled by Shipping and Billing.  Billing is publishing an OrderBilled event which completes the billing part of the order but the order can't be shipped yet because Shipping cannot determine if both the OrderPlaced and OrderBilled events have occurred.  We need a Saga in order to do that which we will do in the next step. 