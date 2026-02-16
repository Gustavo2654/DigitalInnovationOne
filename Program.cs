using MinimalApi;

// *Ponto de entrada da aplicação, onde o host é configurado e a aplicação é iniciada*
IHostBuilder CreateHostBuilder(string[] args){
    return Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(webBuilder =>
        {
            webBuilder.UseStartup<Startup>();
        });
}

CreateHostBuilder(args).Build().Run();