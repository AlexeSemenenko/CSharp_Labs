using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MyCompiler;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Project.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private static List<string> FilesNames = new List<string>();
        private static List<string> FilesTexts = new List<string>();

        private static string ReadAsList(IFormFile file)
        {
            var result = new StringBuilder();
            using (var reader = new StreamReader(file.OpenReadStream()))
            {
                while (reader.Peek() >= 0)
                    result.AppendLine(reader.ReadLine()); 
            }
            return result.ToString();
        }

        [HttpGet]
        public List<string> GetAll()
        {
            return FilesNames;
        }


        [HttpGet("{name}")]
        public object GetByName(string name)
        {
            if (FilesNames.IndexOf(name) == -1)
            {
                return BadRequest("No such file. Please try again");
            }
            
            var index = FilesNames.IndexOf(name);
            return FilesTexts[index];
        }


        [HttpPost]
        public object Add(IFormFile file)
        {
            var fileName = file.FileName;
            
            if (FilesNames.IndexOf(fileName) != -1)
            {
                return BadRequest("File with the same name already exists");
            }
            
            FilesNames.Add(fileName);
            var text = ReadAsList(file);
            FilesTexts.Add(text);
            
            return Ok("Added successfully");
        }


        [HttpPatch]
        public object Update(IFormFile file)
        {
            var fileName = file.FileName;
            
            if (FilesNames.IndexOf(fileName) == -1)
            {
                return BadRequest("No such file. Please try again");
            }
            
            var index = FilesNames.IndexOf(fileName);
            var text = ReadAsList(file);
            FilesTexts[index] = text;
            
            return Ok("Updated successfully");
        }


        [HttpDelete("{name}")]
        public object DeleteByName(string name)
        {
            if (FilesNames.IndexOf(name) == -1)
            {
                return BadRequest("No such file. Please try again");
            }
            
            var index = FilesNames.IndexOf(name);
            FilesNames.RemoveAt(index);
            FilesTexts.RemoveAt(index);
            
            return Ok("Removed successfully");
        }

        [HttpPost("{name}/start")]
        public object Start(string name, double[] props)
        {
            if (FilesNames.IndexOf(name) == -1)
            {
                return BadRequest("No such file. Please try again");
            }
            
            var index = FilesNames.IndexOf(name);
            var codeText = FilesTexts[index];

            try
            {
                var method = Compiler.compile(codeText);
                var result = Compiler.run(method, props);

                return result.ToString();
            }
            catch (Exception exception)
            {
                return BadRequest(exception.Message);
            }
        }
    }
}
