using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Text.RegularExpressions;
using System.Linq;


namespace MoogleEngine
{
    public static class Moogle
    {   
        //variables que me serviran mas adelante. Las que estan sin valores asignados en realidad se definen antes de hacer la busqueda en Program.cs
        public static Dictionary <string, string[]> sinonimos = new Dictionary <string, string[]> ();
        public static Dictionary <string, int> tfGlobal = new Dictionary <string, int> ();
        public static string [] fileEntries = new string [0];
        public static char[] charSeparators = new char[] {'~','*','!','^','º','ª','|','"','@','·','#','$','%','€','&','¬','/','(',')','=','?','¿','¡','+','-','^','`','[',']','}','Ç','¨','´','{',';',',',':','.','-','_','<','>',' '};
        public static char[] charSeparators_to_query = new char[] {'º','ª','|','"','@','·','#','$','%','€','&','¬','/','(',')','=','?','¿','¡','+','-','^','`','[',']','}','Ç','¨','´','{',';',',',':','.','-','_','<','>',' '};
        static Dictionary <string, string[]> Sinonimos = new Dictionary <string, string[]> ();
        public static string [] cuerpo = new string [0];

        
        //metodo super especial y magico para contar apariciones de una palabra en un texto
        public static int Count(this string input, string substr) {
            return Regex.Matches(input, substr).Count;
        }
        

        //Hacer sugerencia si en el query ninguna palabra es valida.
        static string Search_Sugesstion(string[] arrquery) {
            
            string suggestion = "";
            //Hace la diferencia entre las palabras del query con todas las existentes
            // y luego me quedo con una frase que tiene las palabras mas parecidas a las del query
            
            string [] best_suggestion = new string [arrquery.Length];
            
            
            for ( int i = 0; i < arrquery.Length; i++)
            {
                int min_difference = int.MaxValue;
                foreach (var word2 in tfGlobal) {   
                    int new_difference = MoogleEngine.Tools.Levenshtein(arrquery[i], word2.Key);
                    if (min_difference > new_difference) {
                        min_difference = new_difference;
                        best_suggestion[i] = word2.Key;
                    }
                }            
            }        
            for (int i = 0; i < best_suggestion.Length; i++) {
                suggestion = suggestion + " " + best_suggestion[i];
            }                   
            return suggestion;     
        }
        
        //metodo principal de búsqueda
        static SearchItem[] Search(string [] arrquery) {
       
            List <SearchItem> LItems = new List <SearchItem> ();

            //el idf (valor de la palabra segun que tan rebuscada es) de cada palabra lo guardo aqui para mas tarde
            float [] idf = new float [arrquery.Length];
            
            for (int i = 0; i < arrquery.Length; i++)
            {
                int is_in_document = 0;
                    foreach(string _content in cuerpo) {

                        // en caso de que por alguna razon se haya puesto mas de un operador, los boviaremos y nos quedamos con la palabra
                        string [] input = arrquery[i].Split(charSeparators);
                        string palabra_exacta = " ";
                        foreach (string item in input)
                        {
                            if (item.Length>1)
                            {
                                palabra_exacta = " " + item + " ";
                                break;
                            }
                        }
                        if(_content.Contains(palabra_exacta)) is_in_document++;
                    }
                        idf[i] = (float)Math.Log((float)cuerpo.Count() / ((float)1 + is_in_document));
            }
                // busco en los archivos y si encuentro en alguno calculo el tfidf 
            for(int k = 0; k < fileEntries.Length; k++) {
                
                string file_name = fileEntries[k];
                string content = cuerpo[k];
            
                for (int i = 0; i < arrquery.Length; i++)
                {
                    int scalar = 1;
                    string Palabra = " " + arrquery[i] + " ";
                    
                    //operadores de busqueda
                    if (arrquery[i] == "~" || arrquery[i] == "*" || arrquery[i] == "!" || arrquery[i] == "^") continue;
                    

                    // Palabras que no tienen que aparecer
                    if (arrquery[i].Contains('!')) {
                        
                        int pos = Palabra.IndexOf('!');
                        Palabra = Palabra.Remove(pos,1);
                        
                        if (content.Contains(Palabra)) {
                            foreach (var element in LItems)
                                if (element.Title == file_name)
                                    LItems.Remove(element);
                            break;
                        }
                    }
                    //palabras que tienen que aparecer
                    if (arrquery[i].Contains('^')) {

                        int pos = Palabra.IndexOf('^');
                        Palabra = Palabra.Remove(pos,1);
                        
                        if (!content.Contains(Palabra)) {
                            foreach (var element in LItems)
                                if (element.Title == file_name)
                                    LItems.Remove(element);
                            break;
                        }
                    }
                    //Incrementa la importancia de la palabra
                    if (arrquery[i].Contains('*')) {
                        
                        foreach (char s in arrquery[i])
                            if (s == '*') scalar++;
                            
                        Palabra = Palabra.Remove(1, scalar-1);
                    } 

                    // Core del metodo
                    float t = content.Count(Palabra);
                    if(t > 0 && idf[i] > 0.9) {
                        string [] arrfileword = content.Split(charSeparators);
                        float D = arrfileword.Count();

                        float tf = t / D;
                    
                        float tfidf = tf * idf[i] * scalar;

                        // hallar el snippet
                        
                        int pos = content.IndexOf(Palabra);
                        int posInicial = pos - 50;
                        int longitud = 100 + Palabra.Length;
                        if (posInicial < 0) posInicial = 0;
                        if (longitud > content.Length - posInicial) longitud = content.Length - posInicial;
                        
                        //ojo con el content en minusculas y sin espacios
                        
                        string snippet = content.Substring(posInicial, longitud);
                        string title = Path.GetFileName(file_name);
                        title = title.Remove(title.Length-4,4);
                        
                        LItems.Add(new SearchItem(title, snippet, (float)tfidf));
                    }
                }
            }
            //Una vez enlistados todos los resultados de busqueda, procedo a alterar el score
            //beneficiando  a los que tengan las palabras afectadas por el operadorsegun su cercanía
            for (int i = 0; i < arrquery.Length; i++)
                if (arrquery[i] == "~")
                    LItems = MoogleEngine.Tools.Operador_distancia(LItems, arrquery[i-1], arrquery[i+1]);
                
            return MoogleEngine.Tools.Unificar(LItems.OrderByDescending(o=>o.Score).ToList());    
        }   

        public static SearchResult Query(string query) {

            
            string temp = query;
            temp = MoogleEngine.Tools.FixedText(temp);
            string [] arrquery = temp.Split(charSeparators_to_query);
            temp = "";
            //Extraer los sinonimos
            
            for (int i = 0; i < arrquery.Length; i++)
                foreach (var synonymous in sinonimos){
                    if(arrquery[i] == synonymous.Key){
                        foreach (string word in synonymous.Value)
                            temp += word + " ";

                    break;
                    }             
                }           
            
            //buscar el query y los sinonimos del mismo. puede que las palabras del query no existan pero sus sinonimos sí
            string [] Temp = temp.Split(' '); 
            string [] to_search = arrquery.Concat(Temp).ToArray();
                        
            SearchItem[] items = Search(to_search);
            string suggestion = Search_Sugesstion(arrquery);   
        
            return new SearchResult(items, suggestion);
        }
    }
}