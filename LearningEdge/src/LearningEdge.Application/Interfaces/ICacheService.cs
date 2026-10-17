using System;
using System.Collections.Generic;
using System.Text;

namespace LearningEdge.Application.Interfaces;

public interface ICacheService
{
    Task SetAsync(string key, string value, TimeSpan? expiry = null);
    Task<string?> GetAsync(string key);
}
