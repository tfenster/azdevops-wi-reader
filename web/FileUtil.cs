using System;
using System.Threading.Tasks;
using Microsoft.JSInterop;

namespace web
{
    public static class FileUtil
    {
        public async static ValueTask<object> SaveAs(this IJSRuntime js, string filename, byte[] data)
            => await js.InvokeAsync<object>(
                "saveAsFile",
                filename,
                Convert.ToBase64String(data));
    }
}