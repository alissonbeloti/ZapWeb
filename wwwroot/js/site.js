var connection = new signalR.HubConnectionBuilder().withUrl('/ZapWebHub').build();
var nomeGrupo = "";

function conectionStart() {
    connection.start().then(function () {
        console.info("Conectou no Hub.");
        HabilitarLogin();
        HabilitarCadastro();
        HabilitarConversacao();
    })
        .catch(function (err) {
            console.error(err.toString());
            if (connection.state == 0) {
                setTimeout(conectionStart, 5000);
            }
        });
}

connection.onclose(async () => { await conectionStart(); });

function HabilitarLogin() {
    //
    var formLogin = document.getElementById("form-login");

    if (formLogin != null) {
        var usuario = sessionStorage.getItem("usuario");
        if (usuario != null) {
            window.location.href = "/Home/Conversacao";
        }

        var btnAcessar = document.getElementById("acessar");
        btnAcessar.addEventListener("click", function () {
            var email = document.getElementById("email").value;
            var senha = document.getElementById("senha").value;

            var usuario = {
                Email: email,
                Senha: senha,
            };

            connection.invoke("Login", usuario)
                .then(function () {
                    //
                })
                .catch(function (e) {
                    console.error(e.toString());
                }
                );
        });
        var btnCadastrar = document.getElementById("cadastrar");
        btnCadastrar.addEventListener("click", function () {
            window.location.href = "/Home/Cadastro";
        });
    }

    connection.on("ReceberLogin", function (sucesso, usuario, msg) {
        if (sucesso) {
            console.info('Logou');
            sessionStorage.setItem("usuario", JSON.stringify(usuario));
            window.location.href = "/Home/Conversacao";
        }
        else {
            var mensagem = document.getElementById("mensagem");
            mensagem.innerText = msg;
        }
    })
}

function HabilitarCadastro() {
    var formCadastro = document.getElementById("container-cadastro");

    if (formCadastro != null) {
        var btnCadastrar = document.getElementById("btnCadastrar");
        btnCadastrar.addEventListener("click", function () {
            var nome = document.getElementById("nome").value;
            var email = document.getElementById("email").value;
            var senha = document.getElementById("senha").value;

            var usuario = {
                Nome: nome,
                Email: email,
                Senha: senha,
            };

            connection.invoke("Cadastrar", usuario)
                .then(function () {
                    console.info("Cadastrou")
                })
                .catch(function (e) {
                    console.error(e.toString());
                }
                );
        })
    }
    connection.on("ReceberCadastro", function (sucesso, usuario, msg) {
        var mensagem = document.getElementById("mensagem");
        if (sucesso) {
            document.getElementById("nome").value = "";
            document.getElementById("email").value = "";
            document.getElementById("senha").value = "";
        }
        mensagem.innerText = msg;
    });
}



function HabilitarConversacao() {
    var telaConversacao = document.getElementById("tela-conversacao");
    if (telaConversacao != null) {
        MonitorarConnectionId();
        MonitorarListaDeUsuarios();
        EnviarReceberMensagem();
        AbrirGrupo();
        OfflineDetect();
    }
}

function OfflineDetect() {
    window.addEventListener("beforeunload", function (event) {
        connection.invoke("DelConnectionIdDoUsuario", JSON.parse(this.sessionStorage.getItem("usuario")));
        event.returnValue = "Tem certeza que deseja sair?";
    });
}

function AbrirGrupo(){
    connection.on("AbrirGrupo", function (nomeGrup, mensagens) {
        nomeGrupo = nomeGrup;
        
        var container = document.querySelector(".container-messages");
        container.innerHTML = "";
        var container = document.querySelector(".container-messages");
        var mensagemHtml = "";
        for (i = 0; i < mensagens.length; i++) {
            console.info(mensagens[i].texto);
            mensagemHtml = '<div class="message message-' + (mensagens[i].usuarioObj.id == JSON.parse(sessionStorage.getItem("usuario")).id ? "right" : "left") +
                '"><div class="message-head"><img src="/imagem/chat.png" /> '
                + mensagens[i].usuarioObj.nome + '</div><div class="message-message">' + mensagens[i].texto + '</div></div >';
            container.innerHTML += mensagemHtml;
        }
        document.querySelector(".container-button").style.display = "flex";//display: flex;
    });
}
function EnviarReceberMensagem() {
    var btnEnviar = document.getElementById("btnEnviar");

    btnEnviar.addEventListener("click", function() {
        var mensag = document.getElementById("mensagem");
        connection.invoke("EnviarMensagem", JSON.parse(sessionStorage.getItem("usuario")), mensag.value, nomeGrupo);
        mensag.value = '';
    });

    connection.on("ReceberMensagem", function (mensagem, nomeDoGrupo) {
        //console.info(nomeDoGrupo + " - " + nomeGrupo)
        if (nomeGrupo == nomeDoGrupo) {
            var container = document.querySelector(".container-messages");

            var mensagemHtml = '<div class="message message-' + (mensagem.usuarioObj.id == JSON.parse(sessionStorage.getItem("usuario")).id ? "right" : "left") +
                '"><div class="message-head"><img src="/imagem/chat.png" /> '
                + mensagem.usuarioObj.nome + '</div><div class="message-message">' + mensagem.texto + '</div></div >';
            container.innerHTML += mensagemHtml;
        }
    });
}

function MonitorarListaDeUsuarios() {
    connection.invoke("ObterListaUsuarios");
    connection.on("ReceberListaUsuarios", function (usuarios) {
        var html = "";
        console.info(usuarios); 
        var usuarioLogado = JSON.parse(sessionStorage.getItem("usuario"));
        for (var i = 0; i < usuarios.length; i++) {
            if (usuarios[i].id !== usuarioLogado.id) {
                html += '<div class="container-user-item"><img src="/imagem/logo.png" style="width: 20%;" /><div><span>' + usuarios[i].nome + (usuarios[i].isOnline ? " online" : " offline") + '<BR /></span><span class="email">' + usuarios[i].email + '</span></div></div>'
            }
        }
        var users = document.getElementById("users");
        users.innerHTML = html;

        var containers = users.querySelectorAll(".container-user-item");
        for (var i = 0; i < containers.length; i++) {
            containers[i].addEventListener("click", function (event) {
                var componente = event.target || event.srcElement;

                var emailUsuarioUm = JSON.parse(sessionStorage.getItem("usuario")).email;
                var emailUsuarioDois = componente.parentElement.querySelectorAll(".email")[0].innerText;

                connection.invoke("CriarOuAbrirGrupo", emailUsuarioUm, emailUsuarioDois);
            });
        }   
    });
}
function MonitorarConnectionId() {
    var telaConversacao = document.getElementById("tela-conversacao");
    if (telaConversacao != null) {
        var usuario = JSON.parse(sessionStorage.getItem("usuario"));
        if (usuario == null) {
            window.location.href = "/Home/Login";
        }
        else {
            connection.invoke("AtualizarConnectionIdDoUsuario", usuario);
        }
        var btnSair = document.getElementById("btnSair");
        btnSair.addEventListener("click", function () {
            var usuario = JSON.parse(sessionStorage.getItem("usuario"));
            connection.invoke("Logout", usuario)
                .then(function () {
                    sessionStorage.removeItem("usuario");
                    window.location.href = "/Home/Login";
                });
        });
    }
}
conectionStart();