using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ZapWeb.Models;

namespace ZapWeb.Database
{
    public class ZapContext: DbContext
    {
        public ZapContext(DbContextOptions<ZapContext> options)
            :base (options)
        {

        }

        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Grupo> Grupos { get; set; }
        public DbSet<Mensagem> Mensagems { get; set; }

    }
}
