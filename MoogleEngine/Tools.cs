using System.Text.Json;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace MoogleEngine
{
    public class Tools
    {   
       
        public static Dictionary <string, int> TfGlobal() {
            
            Dictionary <string, int> tfGlobal = new Dictionary <string, int>();
            
            /* Leer todos los txt de fileEntries y hacer la frecuencia de 
            las palabras segun su aparicion en el texto.*/
            
            foreach(string fileName in MoogleEngine.Moogle.fileEntries) {
                string content = File.ReadAllText(fileName);
                content = FixedText(content);          
                string[] arrcontent = content.Split(MoogleEngine.Moogle.charSeparators);
                
                foreach( string key in arrcontent) {
                    if(tfGlobal.ContainsKey(key))
                        tfGlobal[key] += 1 ;
                    else
                        tfGlobal.Add(key, 1 );
                }       
            }
        return tfGlobal;
        }

        public static Dictionary <string, string[]> Sinonimos (){
           
            return JsonSerializer.Deserialize <Dictionary<string, string[]>>(File.ReadAllText("../MoogleEngine/Sinonimos.json"));      
        }
        
        public static string [] Do_synonymous(string [] arrquery){
            string temp = "";
            for (int i = 0; i < arrquery.Length; i++)
                foreach (var synonymous in MoogleEngine.Moogle.sinonimos){
                    if(arrquery[i] == synonymous.Key){
                        foreach (string word in synonymous.Value)
                            temp += word + " ";

                    break;
                    }             
                }           
            return temp.Split(' ');
        }
        
        public static string Si_o_no(){
            if(MoogleEngine.Moogle.Do_synonymous) return "si";
            else return "no";
        }
         static public string [] Archivar(){

            string [] filenames = Directory.GetFiles(@"../Content/", "*.txt");
            string [] readed_files = new string [filenames.Length];  

            for(int i = 0; i < filenames.Length; i++)
            {
                readed_files[i] = File.ReadAllText(FixedText(filenames[i]));
            }
            return readed_files;
        }
        static public string FixedText(string text) {
            
            return text.ToLower().Trim();
            
            }

        //el metodo devuelve la cantidad de cambios minimos para convertir una palabra en otra
        public static int Levenshtein(string word1, string word2) 
        {
            var Length1 = word1.Length;
            var Length2 = word2.Length;

            var matrix = new int[Length1 + 1, Length2 + 1];

            if (Length1 == 0) return Length2;

            if (Length2 == 0) return Length1;

            
            for (var i = 0; i <= Length1; matrix[i, 0] = i++){}
            for (var j = 0; j <= Length2; matrix[0, j] = j++){}

            for (var i = 1; i <= Length1; i++)
                for (var j = 1; j <= Length2; j++) {
                    var cost = (word2[j - 1] == word1[i - 1]) ? 0 : 1;

                    matrix[i, j] = Math.Min(Math.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1), matrix[i - 1, j - 1] + cost);
                }
            
            return matrix[Length1, Length2];
        }

        // La lista que recibe son todos los search items a devolver.
        // el metodo elimina todos los SearchItem sobrantes con el mismo titulo y suma sus scores
        static public SearchItem[] Unificar (List<SearchItem> items) {
        //incrementa el score del documento mientras mas palabras tenga.
          for (int i = 0; i < items.Count; i++) 
            for (int j = i+1; j < items.Count; j++)                   
              if (items.ElementAt(i).Title == items.ElementAt(j).Title){
                
                items.ElementAt(i).Score += items.ElementAt(j).Score;
               
                items.RemoveAt(j);
              }      
          return items.ToArray();
        } 
        
        //no es el metodo mas elegante pero es rapido
        static public List <SearchItem> Operador_distancia (List<SearchItem> resultados, string word1, string word2) {
            //por cada SearchItem verifico si este contiene las dos palabras afectadas por el operador y mejora el score mientras mas cerca este
            for (int i = 0; i < MoogleEngine.Moogle.fileEntries.Length; i++){
                for (int j = 0; j < resultados.Count; j++)
                    if (MoogleEngine.Moogle.fileEntries[i].Contains(resultados[j].Title))
                    {
                        string content = FixedText(File.ReadAllText(MoogleEngine.Moogle.fileEntries[i]));
                        if (content.Contains(word1) && content.Contains(word2))
                        {
                            IEnumerable<int> pos_word1 = Regex.Matches(content, word1).Cast<Match>().Select(m => m.Index);
                            IEnumerable<int> pos_word2 = Regex.Matches(content, word2).Cast<Match>().Select(m => m.Index);

                            float min_distance = int.MaxValue;
                            foreach (int pos1 in pos_word1 ) {
                                foreach (int pos2 in pos_word2) {
                                    int new_distance = Math.Max((pos1 - pos2), (pos2-pos1));
                                    if (min_distance > new_distance) min_distance = new_distance; 
                                }
                            }
                            resultados[j].Score *= (1+(100/min_distance));
                            Console.WriteLine("escalar de distancia-" + 1+(100/min_distance));
                            Console.WriteLine(resultados[j]);
                        }                           
                    }
                
            }
            return resultados;
        }        
    }
}