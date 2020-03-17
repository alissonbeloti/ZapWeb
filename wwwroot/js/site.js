var connection = new signalR.HubConnectionBuilder().withUrl('/ZapWebHub').build();

function conectionStart() {
    connection.start().then(function () {
        console.info("Conectou no Hub.");
        HabilitarCadastro();
        HabilitarLogin();
    })
        .catch(function (err) {
            console.error(err.toString());
            setTimeout(conectionStart(), 5000);
        });
}

connection.onclose(async () => { await conectionStart(); });

function HabilitarLogin() {
    //
    var formLogin = document.getElementById("form-login");

    if (formLogin != null) {
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
                .catch(function (e)
                    {
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

conectionStart();