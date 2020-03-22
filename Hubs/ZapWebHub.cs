using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.SignalR;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;
using ZapWeb.Database;
using ZapWeb.Migrations;
using ZapWeb.Models;

namespace ZapWeb.Hubs
{
    public class ZapWebHub : Hub
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
                await Clients.Caller.SendAsync("ReceberLogin", true, usuarioExiste, "");

                usuarioExiste.IsOnline = true;
                zapContext.Update(usuarioExiste);
                zapContext.SaveChanges();
                await NotificarMudancaListaUsuarios();
            }
        }

        public async Task Logout(Usuario usuario)
        {
            var usuarioDb = zapContext.Usuarios.Find(usuario.Id);
            if (usuarioDb != null)
            {
                usuarioDb.IsOnline = false;
                zapContext.Update(usuarioDb);
                zapContext.SaveChanges();
                //usuarioExiste.Senha = string.Empty;
                await DelConnectionIdDoUsuario(usuario);
                await NotificarMudancaListaUsuarios();
            }
        }

        //AtualizarConnectionIdDoUsuario
        public async Task AtualizarConnectionIdDoUsuario(Usuario usuario)
        {
            if (usuario != null)
            {
                var usuarioDb = zapContext.Usuarios.Find(usuario.Id);
                var connectionsIds = new List<string>();
                if (usuarioDb.ConnectionId?.Length > 0)
                {
                    connectionsIds = JsonConvert.DeserializeObject<List<string>>(usuario.ConnectionId);
                    var connectionIdCurrent = Context.ConnectionId;
                    if (!connectionsIds.Contains(connectionIdCurrent))
                    {
                        connectionsIds.Add(connectionIdCurrent);
                    }
                }
                else
                {
                    connectionsIds.Add(Context.ConnectionId);
                }
                usuarioDb.IsOnline = true;
                usuarioDb.ConnectionId = JsonConvert.SerializeObject(connectionsIds);
                zapContext.Usuarios.Update(usuarioDb);
                await zapContext.SaveChangesAsync();
                var grupos = zapContext.Grupos.Where(g => g.Usuarios.Contains(usuario.Email));
                foreach (var grupo in grupos)
                {
                    foreach (var connectionId in connectionsIds)
                    {
                        await Groups.AddToGroupAsync(connectionId, grupo.Nome);
                    }
                }
            }
        }


        public async Task DelConnectionIdDoUsuario(Usuario usuario)
        {
            if (usuario == null) return;
            Usuario usuarioDb = zapContext.Usuarios.SingleOrDefault<Usuario>(u => u.Id == usuario.Id);

            if (usuarioDb.ConnectionId?.Length > 0)
            {
                var connectionsIds = JsonConvert.DeserializeObject<List<string>>(usuarioDb.ConnectionId);
                var connectionIdCurrent = Context.ConnectionId;
                if (connectionsIds.Contains(connectionIdCurrent))
                {
                    connectionsIds.Remove(connectionIdCurrent);
                }
                usuarioDb.ConnectionId = JsonConvert.SerializeObject(connectionsIds);
                if (connectionsIds.Count == 0) {
                    usuarioDb.IsOnline = false;
                }
                zapContext.Usuarios.Update(usuarioDb);
                await zapContext.SaveChangesAsync();
                if (connectionsIds.Count == 0)
                {
                    await NotificarMudancaListaUsuarios();
                }
                var grupos = zapContext.Grupos.Where(g => g.Usuarios.Contains(usuario.Email));
                foreach (var grupo in grupos)
                {
                    foreach (var connectionId in connectionsIds)
                    {
                        await Groups.RemoveFromGroupAsync(connectionId, grupo.Nome);
                    }
                }
            }

        }
        private async Task NotificarMudancaListaUsuarios()
        {
            await Clients.All.SendAsync("ReceberListaUsuarios", zapContext.Usuarios.ToList());
        }
        //ObterListaUsuarios
        public async Task ObterListaUsuarios()
        {
            var usuarios = zapContext.Usuarios.ToList();

            await Clients.Caller.SendAsync("ReceberListaUsuarios", usuarios);

        }
        //SignalR - grupo tem nome único.
        public async Task CriarOuAbrirGrupo(string email1, string email2)
        {
            string nomeGrupo = CriarNomeGrupo(email1, email2);

            var grupo = zapContext.Grupos.Where(g => g.Nome == nomeGrupo).SingleOrDefault();
            if (grupo == null)
            {
                //var usuario1 = zapContext.Usuarios.FirstOrDefault(u => u.Email == email1);
                //var usuario2 = zapContext.Usuarios.FirstOrDefault(u => u.Email == email2);
                grupo = new Grupo();
                grupo.Nome = nomeGrupo;
                grupo.Usuarios = JsonConvert.SerializeObject(new List<string>() {
                    email1,
                    email2,
                });
                zapContext.Add(grupo);
                await zapContext.SaveChangesAsync();
            }

            //criar o grupo do signalr
            var emails = JsonConvert.DeserializeObject<List<string>>(grupo.Usuarios);
            var usuarios = new List<Usuario>() {
                zapContext.Usuarios.FirstOrDefault(u => u.Email == emails[0]),
                zapContext.Usuarios.FirstOrDefault(u => u.Email == emails[1])
                };
            foreach (var usuario in usuarios)
            {
                var connectionsId = JsonConvert.DeserializeObject<List<string>>(usuario.ConnectionId);
                foreach (var connectionId in connectionsId)
                {
                    await Groups.AddToGroupAsync(connectionId, nomeGrupo);
                }
            }

            var mensagens = zapContext.Mensagems.Where(m => m.NomeGrupo == nomeGrupo).OrderBy(m => m.DataCriacao).ToList();
            foreach (var m in mensagens)
            {
                var usuario = JsonConvert.DeserializeObject<Usuario>(m.Usuario);
                m.UsuarioObj = usuarios.FirstOrDefault(u => u.Email == usuario.Email);
            }
            await Clients.Caller.SendAsync("AbrirGrupo", nomeGrupo, mensagens);
        }

        public async Task EnviarMensagem(Usuario usuario, string mensagem, string nomeGrupo)
        {
            var grupo = zapContext.Grupos.SingleOrDefault(g => g.Nome == nomeGrupo);

            if (!grupo.Usuarios.Contains(usuario.Email))
            {
                throw new Exception("Usuário não pertence ao grupo!");
            }
            var msg = new Mensagem();
            msg.NomeGrupo = nomeGrupo;
            msg.DataCriacao = DateTime.Now;
            msg.Texto = mensagem;
            msg.Usuario = JsonConvert.SerializeObject(usuario);
            msg.UsuarioObj = usuario;
            zapContext.Mensagems.Add(msg);
            await zapContext.SaveChangesAsync();

            await Clients.Group(nomeGrupo).SendAsync("ReceberMensagem", msg, nomeGrupo);  
        }

        private string CriarNomeGrupo(string email1, string email2)
        {
            var lista = new List<string>() { email1, email2 };
            var listaOrdenada = lista.OrderBy(a => a).ToList();
            string retorno = "";
            foreach (var item in listaOrdenada)
            {
                retorno += item;
            }
            return retorno;
        }
    }
}
