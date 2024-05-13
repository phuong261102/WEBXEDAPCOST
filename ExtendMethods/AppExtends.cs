using System.Net;
using System.Net.Http;
using App.Data;
using App.Models;
using Microsoft.AspNetCore.Identity;
namespace App.ExtendMethods
{
    public static class AppExtends
    {
        public static void AddStatucCodePage(this IApplicationBuilder app)
        {

            app.UseStatusCodePages(appError =>
            {
                appError.Run(async context =>
                {
                    var response = context.Response;
                    var code = response.StatusCode;
                    var content =
                                @$"Có lỗi xảy ra : {code} - {(HttpStatusCode)code}";


                    await response.WriteAsJsonAsync(content);
                });
            }); // error 400 ->
        }
    }


}