﻿using API_Peliculas.Data;
using API_Peliculas.Modelos;
using API_Peliculas.Modelos.Dtos;
using API_Peliculas.Repositorio.IRepositorio;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using XSystem.Security.Cryptography;

namespace API_Peliculas.Repositorio
{
    //34º
    public class UsuarioRepositorio : IUsuarioRepositorio
    {
        private readonly ApplicationDbContext _bd;
        private string claveSecreta; //37º - 3/4
        private readonly UserManager<AppUsuario> _userManager; //55º
        private readonly RoleManager<IdentityRole> _roleManager; //56º
        private readonly IMapper _mapper;

        public UsuarioRepositorio(ApplicationDbContext bd, IConfiguration config,
            UserManager<AppUsuario> userManager, RoleManager<IdentityRole> roleManager, IMapper mapper)
        {
            _bd = bd;
            claveSecreta = config.GetValue<string>("ApiSettings:Secreta"); //37º - 4/4
            _roleManager = roleManager;
            _userManager = userManager;
            _roleManager = roleManager;
            _mapper = mapper;
        }

        public AppUsuario GetUsuario(string usuarioId)
        {
            return _bd.AppUsuario.FirstOrDefault(u => u.Id == usuarioId);
        }

        public ICollection<AppUsuario> GetUsuarios()
        {
            return _bd.AppUsuario.OrderBy(u => u.UserName).ToList();
        }

        public bool IsUniqueUser(string usuario)
        {
            var usuariobd = _bd.AppUsuario.FirstOrDefault(u => u.UserName == usuario);
            if (usuariobd == null)
            {
                return true;
            }
            return false;            
        }

        public async Task<UsuarioDatosDto> Registro(UsuarioRegistroDto usuarioRegistroDto)
        {
            AppUsuario usuario = new AppUsuario()
            {
                UserName = usuarioRegistroDto.NombreUsuario,
                Email = usuarioRegistroDto.NombreUsuario,
                NormalizedEmail = usuarioRegistroDto.NombreUsuario.ToUpper(),
                Nombre = usuarioRegistroDto.Nombre
            };

            var result = await _userManager.CreateAsync(usuario, usuarioRegistroDto.Password);
            if (result.Succeeded)
            {
                //SOLO PARA UNA PRIMERA VEZ Y CREAR LOS ROLES
                if (!_roleManager.RoleExistsAsync("admin").GetAwaiter().GetResult())
                {
                    await _roleManager.CreateAsync(new IdentityRole("admin"));
                    await _roleManager.CreateAsync(new IdentityRole("registrado"));
                }

                await _userManager.AddToRoleAsync(usuario, "admin");
                var usuarioRetornado = _bd.AppUsuario.FirstOrDefault(u => u.UserName == usuarioRegistroDto.NombreUsuario);
                //OPCION 1
                //return new UsuarioDatosDto()
                //{
                //    Id = usuarioRetornado.Id,
                //    UserName = usuarioRetornado.UserName,
                //    Nombre = usuarioRetornado.Nombre
                //};
                //OPCION 2
                return _mapper.Map<UsuarioDatosDto>(usuarioRetornado);

            }

            return new UsuarioDatosDto();
        }

        //36º
        public async Task<UsuarioLoginRespuestaDto> Login(UsuarioLoginDto usuarioLoginDto)
        {
            var usuario = _bd.AppUsuario.FirstOrDefault(
                u => u.UserName.ToLower() == usuarioLoginDto.NombreUsuario.ToLower());
            bool IsValida = await _userManager.CheckPasswordAsync(usuario, usuarioLoginDto.Password);

            if (usuario == null || IsValida)
            {
                return new UsuarioLoginRespuestaDto()
                {
                    Usuario = null,
                    Token = ""
                };
            }

            var roles = await _userManager.GetRolesAsync(usuario);

            var manejadorToken = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(claveSecreta); //37º - 1/4
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
                    new Claim(ClaimTypes.Name, usuario.UserName.ToString()),
                    new Claim(ClaimTypes.Role, roles.FirstOrDefault())
                }),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = manejadorToken.CreateToken(tokenDescriptor);

            UsuarioLoginRespuestaDto usuarioLoginRespuestaDto = new UsuarioLoginRespuestaDto()
            {
                Usuario = _mapper.Map<UsuarioDatosDto>(usuario),
                Token = manejadorToken.WriteToken(token)
            };

            return usuarioLoginRespuestaDto;
        }
    }
}
