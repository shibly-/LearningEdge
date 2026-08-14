using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.ValueObjects;

public class Address
{
    public string Address1 { get; }
    public string Address2 { get; }
    public string City { get; }
    public string State { get; }
    public string Country { get; }
    public string Zip { get; }

    public Address(string address1, string address2, string city, string state, string country, string zip)
    {
        if (string.IsNullOrWhiteSpace(address1))
        {
            throw new ArgumentException("Address1 is required.");
        }

        if (string.IsNullOrWhiteSpace(city))
        {
            throw new ArgumentException("City is required.");
        }

        if (string.IsNullOrWhiteSpace(country))
        {
            throw new ArgumentException("Country is required.");
        }

        Address1 = address1;
        Address2 = address2;
        City = city;
        State = state;
        Country = country;
        Zip = zip;
    }

    // Equality by value
    public override bool Equals(object? obj) =>
        obj is Address other &&
        Address1 == other.Address1 &&
        Address2 == other.Address2 &&
        City == other.City &&
        State == other.State &&
        Country == other.Country &&
        Zip == other.Zip;

    public override int GetHashCode() =>
        HashCode.Combine(Address1, Address2, City, State, Country, Zip);

    public override string ToString() =>
        $"{Address1}, {Address2}, {City}, {State}, {Country}, {Zip}";
}
