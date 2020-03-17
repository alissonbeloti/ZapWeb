using System.Linq;
using System.Threading.Tasks;

using Microsoft.AspNetCore.SignalR;
using ZapWeb.Database;
using ZapWeb.Models;

namespace ZapWeb.Hubs
{
    public class ZapWebHub: Hub
    {
        private readonly ZapContext zapContext;

        public ZapWebHub(ZapContext zapContext)
        {
            this.zapContext = zapContext;
        }
        public async Task Cadastrar(Usuario usuario)
        {
            var existeUser = zapContext.Usuarios.Where(u => u.Email == usuario.Email).Any();

            if (existeUser)
            {
                await Clients.Caller.SendAsync("ReceberCadastro", false, null, "Email já cdastrado!");
            }
            else
            {
                zapContext.Usuarios.Add(usuario);
                zapContext.SaveChanges();
                await Clients.Caller.SendAsync("ReceberCadastro", true, usuario, "Cadastrado com sucesso!");
            }
        }
        public async Task Login(Usuario usuario)
        {
            var usuarioExiste = zapContext.Usuarios.Where(u => u.Email == usuario.Email && u.Senha == usuario.Senha).SingleOrDefault();

            if (usuarioExiste == null)
            {
                await Clients.Caller.SendAsync("ReceberLogin", false, null, "Email ou senha incorretos!");
            }
            else
            {
                //usuarioExiste.Senha = string.Empty;
                await Clients.Caller.SendAsync("ReceberLogin", true, usuarioExiste, "");
            }
        }
    }
}
