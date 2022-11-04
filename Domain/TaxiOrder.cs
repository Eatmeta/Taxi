using System;
using System.Globalization;
using System.Linq;
using Ddd.Infrastructure;

namespace Ddd.Taxi.Domain
{
    // In real aplication it whould be the place where database is used to find driver by its Id.
    // But in this exercise it is just a mock to simulate database
    public class DriversRepository
    {
        public void FillDriverToOrder(Driver driver)
        {
            if (driver.Id == 15)
            {
                driver.Id = 15;
                driver.Name = new PersonName("Drive", "Driverson");
                driver.Car.Model = "Lada sedan";
                driver.Car.Color = "Baklazhan";
                driver.Car.PlateNumber = "A123BT 66";
            }
            else
                throw new Exception("Unknown driver id " + driver.Id);
        }
    }

    public class TaxiApi : ITaxiApi<TaxiOrder>
    {
        private readonly DriversRepository _driversRepo;
        private readonly Func<DateTime> _currentTime;
        private int _idCounter;

        public TaxiApi(DriversRepository driversRepo, Func<DateTime> currentTime)
        {
            _driversRepo = driversRepo;
            _currentTime = currentTime;
        }

        public TaxiOrder CreateOrderWithoutDestination(string firstName, string lastName, string street,
            string building)
        {
            return
                new TaxiOrder(_idCounter, new PersonName(firstName, lastName),
                    new Address(street, building), new Address("", ""), _currentTime());
        }

        public void UpdateDestination(TaxiOrder order, string street, string building)
        {
            order.UpdateDestination(new Address(street, building));
        }

        public void AssignDriver(TaxiOrder order, int driverId)
        {
            if (order.Driver.Id != 0)
                throw new InvalidOperationException("The driver has already assigned!");
            
            order.AssignDriver(driverId, _currentTime);
            _driversRepo.FillDriverToOrder(order.Driver);
        }

        public void UnassignDriver(TaxiOrder order)
        {
            if (order.Driver.Id == 0)
                throw new InvalidOperationException("Cannot unassign a driver. TaxiOrderStatus: WaitingForDriver");
            order.UnassignDriver();
        }


        public string GetDriverFullInfo(TaxiOrder order)
        {
            if (order.Status == TaxiOrderStatus.WaitingForDriver) return null;
            return string.Join(" ",
                "Id: " + order.Driver.Id,
                "DriverName: " + FormatName(order.Driver.Name.FirstName, order.Driver.Name.LastName),
                "Color: " + order.Driver.Car.Color,
                "CarModel: " + order.Driver.Car.Model,
                "PlateNumber: " + order.Driver.Car.PlateNumber);
        }

        public string GetShortOrderInfo(TaxiOrder order)
        {
            return string.Join(" ",
                "OrderId: " + order.Id,
                "Status: " + order.Status,
                "Client: " + FormatName(order.ClientName.FirstName, order.ClientName.LastName),
                "Driver: " + FormatName(order.Driver.Name.FirstName, order.Driver.Name.LastName),
                "From: " + FormatAddress(order.Start.Street, order.Start.Building),
                "To: " + FormatAddress(order.Destination.Street, order.Destination.Building),
                "LastProgressTime: " + GetLastProgressTime(order).ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        }

        private DateTime GetLastProgressTime(TaxiOrder order)
        {
            if (order.Status == TaxiOrderStatus.WaitingForDriver) return order.CreationTime;
            if (order.Status == TaxiOrderStatus.WaitingCarArrival) return order.DriverAssignmentTime;
            if (order.Status == TaxiOrderStatus.InProgress) return order.StartRideTime;
            if (order.Status == TaxiOrderStatus.Finished) return order.FinishRideTime;
            if (order.Status == TaxiOrderStatus.Canceled) return order.CancelTime;
            throw new NotSupportedException(order.Status.ToString());
        }

        private string FormatName(string firstName, string lastName)
        {
            if (string.IsNullOrEmpty(firstName) && string.IsNullOrEmpty(lastName))
                return "";
            return string.Join(" ", new[] {firstName, lastName}.Where(n => n != null));
        }

        private string FormatAddress(string street, string building)
        {
            if (string.IsNullOrEmpty(street) && string.IsNullOrEmpty(building))
                return "";
            return string.Join(" ", new[] {street, building}.Where(n => n != null));
        }

        public void Cancel(TaxiOrder order)
        {
            if (order.StartRideTime != DateTime.MinValue)
                throw new InvalidOperationException("Cannot unassign a driver. The ride has already started!");
            order.Cancel(_currentTime);
        }

        public void StartRide(TaxiOrder order)
        {
            if (order.Driver.Id == 0)
                throw new InvalidOperationException("The driver must be assigned!");
            order.StartRide(_currentTime);
        }

        public void FinishRide(TaxiOrder order)
        {
            if (order.StartRideTime == DateTime.MinValue)
                throw new InvalidOperationException("At first you need to start ride!");
            order.FinishRide(_currentTime);
        }
    }

    public class TaxiOrder : Entity<int>
    {
        private readonly Driver _defaultDriver = 
            new Driver(0, new PersonName(null, null), new Car(null, null, null));

        internal new readonly int Id;
        public PersonName ClientName { get; }
        public Address Start { get; }
        public Address Destination { get; private set; }
        public Driver Driver { get; }
        public TaxiOrderStatus Status { get; private set; }
        public DateTime CreationTime { get; }
        public DateTime DriverAssignmentTime { get; private set; }
        public DateTime CancelTime { get; private set; }
        public DateTime StartRideTime { get; private set; }
        public DateTime FinishRideTime { get; private set; }

        public TaxiOrder(int id, PersonName client, Address start, Address destination, DateTime currentTime) : base(id)
        {
            Id = id;
            ClientName = client;
            Start = start;
            Destination = destination;
            CreationTime = currentTime;
            Driver = _defaultDriver;
        }

        public void AssignDriver(int driverId, Func<DateTime> currentTime)
        {
            Driver.Id = driverId;
            DriverAssignmentTime = currentTime();
            Status = TaxiOrderStatus.WaitingCarArrival;
        }
        
        public void UnassignDriver()
        {
            if (StartRideTime != DateTime.MinValue)
                throw new InvalidOperationException("Cannot unassign a driver. The ride has already started!");
            Driver.Name = new PersonName(null, null);
            Driver.Car.Model = null;
            Driver.Car.Color = null;
            Driver.Car.PlateNumber = null;
            Status = TaxiOrderStatus.WaitingForDriver;
        }

        public void Cancel(Func<DateTime> currentTime)
        {
            Status = TaxiOrderStatus.Canceled;
            CancelTime = currentTime();
        }

        public void UpdateDestination(Address adress)
        {
            Destination = new Address(adress.Street, adress.Building);
        }

        public void StartRide(Func<DateTime> currentTime)
        {
            Status = TaxiOrderStatus.InProgress;
            StartRideTime = currentTime();
        }

        public void FinishRide(Func<DateTime> currentTime)
        {
            Status = TaxiOrderStatus.Finished;
            FinishRideTime = currentTime();
        }
    }

    public class Car : ValueType<Car>
    {
        public string Color { get; set; }
        public string Model { get; set; }
        public string PlateNumber { get; set; }

        public Car(string carColor, string carModel, string carPlateNumber)
        {
            Color = carColor;
            Model = carModel;
            PlateNumber = carPlateNumber;
        }
    }

    public class Driver : Entity<int>
    {
        public new int Id { get; set; }
        public PersonName Name { get; internal set; }
        public Car Car { get; }

        public Driver(int id, PersonName driverName, Car car) : base(id)
        {
            Id = id;
            Name = driverName;
            Car = car;
        }
    }
}