using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseStaticFiles();

app.UseRouting();

app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

 

MoogleEngine.Moogle.fileEntries = Directory.GetFiles(@"../Content/", "*.txt");
MoogleEngine.Moogle.tfGlobal = MoogleEngine.Tools.TfGlobal();
MoogleEngine.Moogle.sinonimos = MoogleEngine.Tools.Sinonimos();
MoogleEngine.Moogle.cuerpo = MoogleEngine.Tools.Archivar();



app.Run();
