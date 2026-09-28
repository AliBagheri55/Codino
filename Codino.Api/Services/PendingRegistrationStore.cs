using Codino.Api.Models;
using System.Collections.Concurrent;

namespace Codino.Api.Services
{
    public class PendingRegistrationStore
    {
        private readonly ConcurrentDictionary<string, PendingRegistration>
            _registrations = new();

        public void Save(PendingRegistration registration)
        {
            _registrations[registration.PhoneNumber] = registration;
        }

        public bool TryGet(
            string phoneNumber,
            out PendingRegistration? registration)
        {
            return _registrations.TryGetValue(
                phoneNumber,
                out registration);
        }

        public void Remove(string phoneNumber)
        {
            _registrations.TryRemove(
                phoneNumber,
                out _);
        }
    }
}