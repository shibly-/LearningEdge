using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Domain.ValueObjects;

public class Email
{
    public string Address { get; }

    public Email(string address)
    {
        if (string.IsNullOrWhiteSpace(address))
        {
            throw new ArgumentException("Email cannot be empty.");
        }

        if (!address.Contains('@') || !address.Contains('.'))
        {
            throw new ArgumentException("Invalid email format.");
        }

        Address = address;
    }

    // Equality by value
    public override bool Equals(object? obj) =>
        obj is Email other && Address == other.Address;

    public override int GetHashCode() => Address.GetHashCode();

    public override string ToString() => Address;
}
