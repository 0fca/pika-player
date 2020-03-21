using System;

namespace Claudia.Controllers.Helpers
{
    [Flags]
    public enum UploadType
    {
        VIDEO = 0x01,
        ATTACHEMENT = 0x02
    }
}
