// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json.Linq;

namespace Azure.SignalRBench.Common
{
    public class ScenarioDefinition
    {
        public ClientBehavior ClientBehavior { get; set; }
        public JObject? Detail { get; set; }

        public T? GetDetail<T>()
            where T : ClientBehaviorDetailDefinition
            => Detail?.ToObject<T>();

        public void SetDetail<T>(T? detail)
            where T : ClientBehaviorDetailDefinition
            => Detail = detail == null ? null : JObject.FromObject(detail);
    }
}
