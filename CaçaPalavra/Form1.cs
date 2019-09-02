using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;

namespace CaçaPalavra
{
    public partial class Form1 : Form
    {

        [System.Runtime.InteropServices.DllImport("user32.dll")] //Importação de funções 
        static extern short GetAsyncKeyState(short vKey);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern IntPtr CreateWaitableTimer(IntPtr lpTimerAttributes, bool bManualReset, string lpTimerName);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool SetWaitableTimer(IntPtr hTimer, ref long pDueTime, int lPeriod, IntPtr pfnCompletionRoutine, IntPtr lpArgToCompletionRoutine, bool fResume);

        [System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, ExactSpelling = true)]
        public static extern Int32 WaitForSingleObject(IntPtr handle, uint milliseconds);
        public static uint INFINITE = 0xFFFFFFFF;

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool SetEvent(IntPtr handle);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        public static extern bool ResetEvent(IntPtr handle);

        [System.Runtime.InteropServices.DllImport("kernel32.dll")]
        static extern IntPtr CreateEvent(IntPtr lpEventAttributes, bool bManualReset, bool bInitialState, string lpName);

        /*FUNÇÕES =====> MSDN + PINVOKE*/

        int PalavrasEncontradas = 0;
        int PalavrasAEncontrar = 0;

        IntPtr HWNDForm = IntPtr.Zero; //HWND = Identificador da janela

        List<string[]>      PalavrasGeradas     = new List<string[]>();
        List<List<Label>>   LabelByIndex        = new List<List<Label>>(); /*Pegar Label das letras aleatórias geradas por index. ex: [0][1]*/
        List<Label>         LabelsSelecionados  = new List<Label>();

        Thread ThreaD;

        int Colunas = 16, Linhas = 14;

        IntPtr EventoThread = IntPtr.Zero;
        TimeSpan TimerReset;
        bool ThreadAndamento = false;
        bool FormAberto = true;

        public void TempoAtualiza(Label go)
        {
            IntPtr kkTempo = CreateWaitableTimer(IntPtr.Zero, true, String.Empty);
            EventoThread = CreateEvent(IntPtr.Zero,false, false, string.Empty);

            ThreadAndamento = true;
            while (true)
            {
                TimerReset = TimeSpan.FromMinutes(1);

                if (ThreaD != Thread.CurrentThread)
                {
                    ThreaD = null;
                    ThreaD = Thread.CurrentThread;
                }

                for (int i = 0; TimerReset.TotalSeconds > -1; i++)
                {
                    string temporestante = string.Empty;
                    if (TimerReset.Minutes > 0)
                        temporestante += TimerReset.Minutes + " minutos, e ";
                    if (TimerReset.Seconds > 1)
                        temporestante += TimerReset.Seconds + " segundos.";
                    else
                        temporestante += TimerReset.Seconds + " segundo.";

                    TimerReset = TimeSpan.FromSeconds(TimerReset.TotalSeconds - 1);

                    //go.Text = "Tempo restante: " + temporestante;
                    MethodInvoker mi = delegate () { go.Text = "Tempo restante: " + temporestante; };
                    this.Invoke(mi);
                    long TempoXx = (1 * - (long)1e+7);//-10000000 //1seg
                    /*microssegundo*/
                
                    SetWaitableTimer(kkTempo, ref TempoXx, 0, IntPtr.Zero, IntPtr.Zero, false); //Definir tempo para o temporizador
                    WaitForSingleObject(kkTempo, INFINITE); //Esperar o temporizador
                }
                go.Text = "Tempo restante: esgotado.";

                foreach (Control lLabel in Controls)
                {
                    if (lLabel.Name.StartsWith("Letras"))
                    {
                        lLabel.MouseDown    -= LabelClickMouseClick;
                        lLabel.MouseUp      -= LabelClickMouseUp;
                        lLabel.MouseEnter   -= LabelClickMouseEnter;
                        lLabel.MouseMove    -= delegate (object sender, MouseEventArgs e) { (sender as Control).Capture = false; };
                        lLabel.Enabled      = false;
                    }
                }

                DialogResult Retorno = MessageBox.Show(this, "Você perdeu!\nPressione sim para jogar novamente.", "Tempo esgotado", MessageBoxButtons.YesNo, MessageBoxIcon.Error);

                if (Retorno == DialogResult.Yes)
                {
                    PalavrasEncontradas = 0;
                    ResetarJogo();
                }
                else
                {
                    ThreadAndamento = false;
                    WaitForSingleObject(EventoThread, INFINITE); /*Fica na espera do SetEvent*/
                    if (!FormAberto)
                        Process.GetCurrentProcess().Kill();
                }
            }
        }

        public Form1()
        {
            InitializeComponent();

            PalavrasGeradas.Add(new string[]{  "Neymar", "Goleiro", "Bola", "Grama", "Juiz", "Falta", "Penalti"});                  //futebol = 0
            PalavrasGeradas.Add(new string[]{  "Azul", "Amarelo", "Roxo", "Cinza", "Branco", "Rosa", "Marrom","Preto"});            //cores = 1
            PalavrasGeradas.Add(new string[] { "Maçã", "Uva", "Pera", "Tangerina", "Limão", "Laranja", "Coco" });                   //frutas = 2
            GerarPalavras();

            Label nLabel = new Label
            {
                Text = "Tempo restante: ",
                Location = new Point(340, 280), //Localização do label no form
                Font = new Font(new FontFamily("Calibri"), 10f), //FontFamily = nome fonte, 2°Param = tamanho fonte
                AutoSize = true, //Ajustar o tamanho do label com tamanho do texto
                BackColor = Color.White,
                Name = "cContador"
            };
            Controls.Add(nLabel);

            ThreaD = new Thread(() => TempoAtualiza(nLabel));
            ThreaD.Start();

        }

        void ResetarJogo()
        {
            LabelByIndex.Clear();
            LabelsSelecionados.Clear();
            PalavrasEncontradas = PalavrasAEncontrar = 0;

            Queue<Label> LabelsRemover = new Queue<Label>();

            for (int i = 0; i < Controls.Count; i++)
            {
                
                if (Controls[i].Name.StartsWith("Palavras") || Controls[i].Name.StartsWith("Letras"))
                {
                    Controls[i].Visible = false;
                    LabelsRemover.Enqueue(Controls[i] as Label);
                }
                
            }

            for (; LabelsRemover.Count > 0;)
            {
                LabelsRemover.Peek().Dispose(); //peek pega o primeiro elemento
                Controls.Remove(LabelsRemover.ElementAt(0));
                LabelsRemover.Dequeue(); //remove o primeiro elemento
            }

            EstiloDeVisualizacao = string.Empty;
            GerarPalavras();
        }

        void LimparLabels()
        {
            for (int i = 0; i < LabelsSelecionados.Count; i++)
            {
                LabelsSelecionados[i].BackColor = Color.White;
            }
            LabelsSelecionados.Clear();            
            AtualizaTexto();
        }
        void LabelClickMouseClick(object sender, EventArgs e)
        {
            if ((string)(sender as Label).Tag == "Palavra encontrada.")
                return;

            if (!LabelsSelecionados.Contains(sender as Label))
                LabelsSelecionados.Add(sender as Label);

            Label cLabel = sender as Label;
            cLabel.BackColor = Color.Yellow;
            AtualizaTexto();
        }

        bool AtualizaTexto()
        {
            string PalavraCompleta = string.Empty;
            bool PalavraEncontrada = false;

            for (int i = 0; i < LabelsSelecionados.Count; i++)
                PalavraCompleta += LabelsSelecionados[i].Text;
            for (int i = 0; i < Controls.Count; i++)
            {
                if (Controls[i].Name.StartsWith("Palavras"))
                    if ((Controls[i] as Label).Text.ToUpper() == PalavraCompleta)
                    {
                        Font font = new Font((Controls[i] as Label).Font, FontStyle.Strikeout);
                        /*colocar um traço na palavra de acerto*/

                        (Controls[i] as Label).Font = font;
                        (Controls[i] as Label).Text = String.Format("{0}", (Controls[i] as Label).Text);
                        PalavraEncontrada = true;
                        PalavrasEncontradas++;
                    }
            }

            if (PalavraEncontrada)
            {
                for (int i = 0; i < LabelsSelecionados.Count + 1; i++)
                {
                    if (!(i < LabelsSelecionados.Count))
                    {
                        LabelsSelecionados.Clear();
                        break;
                    }
                    LabelsSelecionados[i].Tag = "Palavra encontrada.";
                    LabelsSelecionados[i].BackColor = Color.YellowGreen;
                }
            }

            label1.Text = "Palavra formada: " + PalavraCompleta;

            if (PalavrasEncontradas >= PalavrasAEncontrar && !InvokeRequired)
            {
                ThreaD.Abort();
                MessageBox.Show(this, "Parabéns, você ganhou!", "Erro", MessageBoxButtons.OK, MessageBoxIcon.Information);
                button1.PerformClick();
            }

            return true;
        }

        int [] LabelsPosXY(Label LabelOb)
        {
            if (LabelOb != null && LabelOb.Name.StartsWith("Letras"))
            {
                int PosX, PosY, PontoCenter;

                string NomeLabel = LabelOb.Name;
                NomeLabel = NomeLabel.Substring(6, NomeLabel.Length - 6);
                PontoCenter = NomeLabel.IndexOf(',');
                PosX = Convert.ToInt32(NomeLabel.Substring(0, PontoCenter));
                PosY = Convert.ToInt32(NomeLabel.Substring(PontoCenter+1, (NomeLabel.Length - PontoCenter)-1));

                return new int[2] { PosX, PosY };
            }

            return null;

        }

        string EstiloDeVisualizacao = string.Empty;

        void LabelClickMouseEnter(object sender, EventArgs e)
        {
            short LBUTTONSTATE = GetAsyncKeyState(0x01); /*0x01 = LBUTTON = LEFTBUTTON DO MOUSE = MOUSE ESQUERDO!!!*/
            /*GetAsyncKeyState - Vericar o estado de uma tecla // < 0 = KeyDown | > 0 KeyUp | 0 nada | -32767 = Click*/
            if ((sender as Control).Name == "Form1")
            {
                if (LBUTTONSTATE >= 0)
                    LimparLabels();
                return;
            }
            if (LBUTTONSTATE < 0)
            {
                if ((string)(sender as Label).Tag == "Palavra encontrada.")
                    return;

                if (!LabelsSelecionados.Contains(sender as Label))
                    LabelsSelecionados.Add(sender as Label);

                Label cLabel = sender as Label;

                if (cLabel.BackColor == Color.White)
                {
                    if(LabelsSelecionados.Count == 2)
                    {

                        int[] posMy = LabelsPosXY(sender as Label);
                        int[] posObj = LabelsPosXY(LabelsSelecionados[0]);

                        if (posMy[0] == posObj[0] && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Vertical";
                        else if (posMy[1] == posObj[1] && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Horizontal";
                        else if(posObj[0] == posMy[0]-1 && posObj[1] == posMy[1]+1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Direita Cima";
                        else if (posObj[0] == posMy[0]+1 && posObj[1] == posMy[1]-1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Esquerda Baixo";
                        else if (posObj[0] == posMy[0] + 1 && posObj[1] == posMy[1] + 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Esquerda Cima";
                        else if (posObj[0] == posMy[0] - 1 && posObj[1] == posMy[1] - 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Direita Baixo";
                        else
                        {
                            EstiloDeVisualizacao = string.Empty;
                            LabelsSelecionados.Remove(sender as Label);
                        }

                        return;
                    }

                    int[] posMy2 = new int[2];
                    int[] posObj2 = new int[2];
                    if (EstiloDeVisualizacao == string.Empty)
                    {
                        if (LabelsSelecionados.Count < 2)
                            return;

                        posMy2 = LabelsPosXY(LabelsSelecionados[0]);
                        posObj2 = LabelsPosXY(LabelsSelecionados[1]);

                        if (posMy2[0] == posObj2[0])
                            EstiloDeVisualizacao = "Vertical";
                        else if (posMy2[1] == posObj2[1])
                            EstiloDeVisualizacao = "Horizontal";
                        else if (posObj2[0] == posMy2[0] - 1 && posObj2[1] == posMy2[1] + 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Direita Cima";
                        else if (posObj2[0] == posMy2[0] + 1 && posObj2[1] == posMy2[1] - 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Esquerda Baixo";
                        else if (posObj2[0] == posMy2[0] + 1 && posObj2[1] == posMy2[1] + 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Esquerda Cima";
                        else if (posObj2[0] == posMy2[0] - 1 && posObj2[1] == posMy2[1] - 1 && ((sender as Label).BackColor = Color.Yellow) != null && AtualizaTexto())
                            EstiloDeVisualizacao = "Diagonal Direita Baixo";
                        else
                        {
                            EstiloDeVisualizacao = string.Empty;
                            LabelsSelecionados.Remove(sender as Label);
                            return;
                        }
                    }
                    else
                    {
                        posMy2 = LabelsPosXY(sender as Label);
                        posObj2 = LabelsPosXY(LabelsSelecionados[0]);
                    }

                    Queue<Label> AdicionarAoLabels = new Queue<Label>();

                    LabelsSelecionados.Remove(sender as Label);

                    if (EstiloDeVisualizacao == "Vertical")
                    {
                        if (posMy2[0] != posObj2[0])
                        {
                            LabelsSelecionados.Remove(sender as Label);
                            return;
                        }

                        if (posMy2[1] > posObj2[1]) //vertical de cima pra baixo
                        {
                            for (int i = posMy2[1]; i > posObj2[1]; i--)
                            {
                                if ((string)LabelByIndex[i][posMy2[0]].Tag != "Palavra encontrada.")
                                {
                                    LabelByIndex[i][posMy2[0]].BackColor = Color.Yellow;
                                    if (!LabelsSelecionados.Contains(LabelByIndex[i][posMy2[0]]))
                                        AdicionarAoLabels.Enqueue(LabelByIndex[i][posMy2[0]]);
                                }
                            }
                        }
                        else //vertical de baixo pra cima
                        {
                            for (int i = posMy2[1]; i < posObj2[1]; i++)
                            {
                                if ((string)LabelByIndex[i][posMy2[0]].Tag != "Palavra encontrada.")
                                {
                                    LabelByIndex[i][posMy2[0]].BackColor = Color.Yellow;
                                    if (!LabelsSelecionados.Contains(LabelByIndex[i][posMy2[0]]))
                                        AdicionarAoLabels.Enqueue(LabelByIndex[i][posMy2[0]]);
                                }
                            }
                        }

                    }
                    else if (EstiloDeVisualizacao == "Horizontal")
                    {

                        if (posMy2[1] != posObj2[1])
                        {
                            LabelsSelecionados.Remove(sender as Label);
                            return;
                        }

                        if (posMy2[0] < posObj2[0]) //horizontal da direita pra esquerda
                        {
                            for (int i = posMy2[0]; i < posObj2[0]; i++)
                            {
                                if ((string)LabelByIndex[posMy2[1]][i].Tag != "Palavra encontrada.")
                                {
                                    LabelByIndex[posMy2[1]][i].BackColor = Color.Yellow;
                                    if (!LabelsSelecionados.Contains(LabelByIndex[posMy2[1]][i]))
                                        AdicionarAoLabels.Enqueue(LabelByIndex[posMy2[1]][i]);
                                }
                            }
                        }
                        else //horizontal da esquerda pra direita
                        {
                            for (int i = posMy2[0]; i > posObj2[0]; i--)
                            {
                                if ((string)LabelByIndex[posMy2[1]][i].Tag != "Palavra encontrada.")
                                {
                                    LabelByIndex[posMy2[1]][i].BackColor = Color.Yellow;
                                    if (!LabelsSelecionados.Contains(LabelByIndex[posMy2[1]][i]))
                                        AdicionarAoLabels.Enqueue(LabelByIndex[posMy2[1]][i]);
                                }
                            }
                        }
                    }
                    else if (EstiloDeVisualizacao == "Diagonal Direita Baixo")
                    {

                        for (int x = posObj2[0], y = posObj2[1]; x < (posMy2[0] + 1) && y < (posMy2[1] + 1); x++, y++)
                        {
                            if ((string)LabelByIndex[y][x].Tag != "Palavra encontrada.")
                            {
                                LabelByIndex[y][x].BackColor = Color.Yellow;
                                if (!LabelsSelecionados.Contains(LabelByIndex[y][x]))
                                    AdicionarAoLabels.Enqueue(LabelByIndex[y][x]);
                            }
                        }
                    }
                    else if (EstiloDeVisualizacao == "Diagonal Direita Cima")
                    {

                        for (int x = posObj2[0], y = posObj2[1]; x < (posMy2[0]+1) && y > (posMy2[1]-1); x++, y--)
                        {
                            if ((string)LabelByIndex[y][x].Tag != "Palavra encontrada.")
                            {
                                LabelByIndex[y][x].BackColor = Color.Yellow;
                                if (!LabelsSelecionados.Contains(LabelByIndex[y][x]))
                                    AdicionarAoLabels.Enqueue(LabelByIndex[y][x]);
                            }
                        }
                    }
                    else if (EstiloDeVisualizacao == "Diagonal Esquerda Cima")
                    {

                        for (int x = posObj2[0], y = posObj2[1]; x > (posMy2[0] - 1) && y > (posMy2[1] - 1); x--, y--)
                        {
                            if ((string)LabelByIndex[y][x].Tag != "Palavra encontrada.")
                            {
                                LabelByIndex[y][x].BackColor = Color.Yellow;
                                if (!LabelsSelecionados.Contains(LabelByIndex[y][x]))
                                    AdicionarAoLabels.Enqueue(LabelByIndex[y][x]);
                            }
                        }
                    }
                    else if (EstiloDeVisualizacao == "Diagonal Esquerda Baixo")
                    {

                        for (int x = posObj2[0], y = posObj2[1]; x > (posMy2[0] - 1) && y < (posMy2[1] + 1); x--, y++)
                        {
                            if ((string)LabelByIndex[y][x].Tag != "Palavra encontrada.")
                            {
                                LabelByIndex[y][x].BackColor = Color.Yellow;
                                if (!LabelsSelecionados.Contains(LabelByIndex[y][x]))
                                    AdicionarAoLabels.Enqueue(LabelByIndex[y][x]);
                            }
                        }
                    }

                    Array AdicionarAoLabels2 = AdicionarAoLabels.ToArray<Label>(); 
                    /*Converter do Queue pra Array para poder reverter as posições*/
                    Array.Reverse(AdicionarAoLabels2); /*Reverter as posições para o texto não sair invertido*/

                    LabelsSelecionados.AddRange((IEnumerable<Label>)AdicionarAoLabels2);
                    /*Adicionar esta coleção de Array para a coleção de List. Tem que fazer o Cast...*/

                    AdicionarAoLabels.Clear();

                }
                else
                {
                    if (LabelsSelecionados.Count > 0)
                    {
                        int meuObj = LabelsSelecionados.IndexOf(sender as Label);
                        for (int i = LabelsSelecionados.Count - 1; i > -1; i--) //retorna
                        {
                            if (i <= meuObj)
                                break;
                            LabelsSelecionados[i].BackColor = Color.White;
                            LabelsSelecionados.RemoveAt(LabelsSelecionados.Count - 1);
                        }
                    }
                }
            }
            AtualizaTexto();
        }
        void LabelClickMouseUp(object sender, MouseEventArgs e)
        {
            EstiloDeVisualizacao = string.Empty;
            LimparLabels();
        }

        enum PalavrasAddTipo
        {
            Vertical,
            Horizontal,
            DiagonalDireitaCima,
            DiagonalDireitaBaixo
        };

        bool AdicionarPalavra(string Palavra, int initPosX, int initPosY, PalavrasAddTipo Maneira)
        {
            int caracteres = Palavra.Length;

            if (Maneira == PalavrasAddTipo.Horizontal)
            {
                if (initPosX + caracteres > Colunas || initPosY < 0 || initPosY > Linhas || initPosX < 0)
                    return false;

                for (int i = 0; i < caracteres; i++)
                {
                    if ((string)LabelByIndex[initPosY][initPosX + i].Tag == "Em uso")
                        return false;
                }
                for (int i = 0; i < caracteres; i++)
                {
                    LabelByIndex[initPosY][initPosX + i].Text = Char.ToUpper(Palavra[i]).ToString();
                    LabelByIndex[initPosY][initPosX + i].Tag = "Em uso";
                }
            }
            else if (Maneira == PalavrasAddTipo.Vertical)
            {
                if (initPosX < 0 || initPosY < 0 || initPosY > Linhas || initPosY + caracteres > Linhas)
                    return false;

                for (int i = 0; i < caracteres; i++)
                {
                    if ((string)LabelByIndex[i + initPosY][initPosX].Tag == "Em uso")
                        return false;
                }

                for (int i = 0; i < caracteres; i++)
                {
                    LabelByIndex[i + initPosY][initPosX].Text = Char.ToUpper(Palavra[i]).ToString();
                    LabelByIndex[i + initPosY][initPosX].Tag = "Em uso";
                }
            }
            else if(Maneira == PalavrasAddTipo.DiagonalDireitaCima)
            {
                if (initPosY < 0 || initPosX < 0)
                    return false;
                if (initPosY - (caracteres-1) < 0 || initPosX + caracteres > Colunas)
                    return false;

                for (int i = 0; i < caracteres; i++)
                {
                    if ((string)LabelByIndex[initPosY - i][initPosX + i].Tag == "Em uso")
                        return false;
                }

                for (int i = 0; i < caracteres; i++)
                {
                    LabelByIndex[initPosY - i][initPosX + i].Text = Char.ToUpper(Palavra[i]).ToString();
                    LabelByIndex[initPosY - i][initPosX + i].Tag = "Em uso";
                }
            }
            else if (Maneira == PalavrasAddTipo.DiagonalDireitaBaixo)
            {
                if (initPosY < 0 || initPosX < 0)
                    return false;
                if (initPosY + caracteres > Linhas || initPosX + caracteres > Colunas)
                    return false;

                for (int i = 0; i < caracteres; i++)
                {
                    if ((string)LabelByIndex[initPosY + i][initPosX + i].Tag == "Em uso")
                        return false;
                }

                for (int i = 0; i < caracteres; i++)
                {
                    LabelByIndex[initPosY + i][initPosX + i].Text = Char.ToUpper(Palavra[i]).ToString();
                    LabelByIndex[initPosY + i][initPosX + i].Tag = "Em uso";
                }
            }
            else
                return false;

            return true;
        }
        void GerarPalavras()
        {

            button1.Enabled = false;

            int UltimoGerado = 0;
            List<string> InternalStr = new List<string>();

            for (int i = 0, g = new Random(Guid.NewGuid().GetHashCode()).Next(0, PalavrasGeradas.Count); i < PalavrasGeradas[g].Length; i++)
            {
                InternalStr.Add(PalavrasGeradas[g][i]);   
            }

            PalavrasAEncontrar = InternalStr.Count;

            int s = 583 / 2 + 60;

            int c = 0;

            foreach (string StrInternal in InternalStr)
            {
                Label nLabel = new Label
                {
                    Name = "Palavras" + c.ToString(),
                    Text = StrInternal,
                    Location = new Point(s, (c * 20) + 12), //Localização do label no form
                    Font = new Font(new FontFamily("Calibri"), 12), //FontFamily = nome fonte, 2°Param = tamanho fonte
                    AutoSize = true, //Ajustar o tamanho do label com tamanho do texto
                    BackColor = Color.White
                };

                if(InvokeRequired)/*thread..*/
                    BeginInvoke(new MethodInvoker(delegate { Controls.Add(nLabel);}));
                else
                    Controls.Add(nLabel);
                c++;
            }

            for (int y = 0; y < Linhas; y++)
            {

                List<Label> LabelCollect = new List<Label>();
                for (int x = 0; x < Colunas; x++)
                {
                    int cNumber = (new Random(Guid.NewGuid().GetHashCode()).Next(65, 90));

                    /*65 ~ 90 => A até Z ----- ASCII TABLE*/

                    while (cNumber == UltimoGerado)
                        cNumber = (new Random(Guid.NewGuid().GetHashCode()).Next(65, 90));

                    UltimoGerado = cNumber;

                    Label nLabel = new Label
                    {
                        Name = "Letras" + x.ToString() + "," + y.ToString(),
                        Text = (Convert.ToChar(cNumber)).ToString(),
                        Location = new Point((x * 20) + 15, (y * 20) + 12), //Localização do label no form
                        Font = new Font("Calibri",10), //FontFamily = nome fonte, 2°Param = tamanho fonte
                        //AutoSize = true, //Ajustar o tamanho do label com tamanho do texto
                        Size = new Size(18, 17),
                        BackColor = Color.White
                    };

                    /*Adiciona eventos para cada label criada*/
                    nLabel.MouseDown    += LabelClickMouseClick;
                    nLabel.MouseUp      += LabelClickMouseUp;
                    nLabel.MouseEnter   += LabelClickMouseEnter;
                    nLabel.MouseMove    += delegate(object sender, MouseEventArgs e) { (sender as Control).Capture = false; };
                    /*Se o mouse down for 'acionado', os outros eventos não funcionam, por isso setar o Capture do Mouse pra false*/

                    if (InvokeRequired)
                        BeginInvoke(new MethodInvoker(delegate { Controls.Add(nLabel); }));
                    else
                        Controls.Add(nLabel); //Adicionar o label ao form
                    LabelCollect.Add(nLabel as Label);
                }

                LabelByIndex.Add(LabelCollect);

            }

           for (int i = 0; i < InternalStr.Count; i++)
            {
                int bx, x, y;
                do{
                    bx = new Random(Guid.NewGuid().GetHashCode()).Next(0, 100);

                    bx = Guid.NewGuid().GetHashCode() % 4;

                    //0 = Vertical, 1 = Horizontal

                    x   = new Random(Guid.NewGuid().GetHashCode()).Next(0, Colunas);
                    y   = new Random(Guid.NewGuid().GetHashCode()).Next(0, Linhas);
                }while(!AdicionarPalavra(InternalStr[i], x, y, (PalavrasAddTipo)bx));
            }

            button1.Enabled = true;

            if(InvokeRequired)
            Form1.ActiveForm.Invalidate();

        }

        private void Form1_Paint(object sender, PaintEventArgs e) /*Atualização dos gráficos do form*/
        {

            try
            {
                Pen pCorPincel = new Pen(Color.DimGray);
                int formLargura = Form1.ActiveForm.Width;
                int formAltura = Form1.ActiveForm.Height;

                Rectangle rcPalavrasGeradas = 
                    new Rectangle((formLargura / 2) + 50, 10, (formLargura / 2 - (+50)) - 15, (formAltura - 10) - 85);

                Rectangle rcPalavrasGerador = 
                    new Rectangle(10, 10, ((formLargura / 2) + 50) - 20, (formAltura - 10) - 65);

                // Graphics gZ = Graphics.Cre;
                e.Graphics.DrawRectangle(pCorPincel, rcPalavrasGeradas);
                e.Graphics.DrawRectangle(pCorPincel, rcPalavrasGerador);


                e.Graphics.Dispose(); //Liberar gráficos da memória
            }
            catch { }

        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //Não precisa pois não vai se vai poder alterar tamanho do janela
            //Form1.ActiveForm.Refresh(); //Atualizar o form para atualizar os desenhos inválidos
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            FormAberto = false;
            SetEvent(EventoThread);
            ThreaD.Abort();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ResetarJogo();
            TimerReset = TimeSpan.FromMinutes(1); //HARDCORE
            if (!ThreadAndamento && ThreaD.ThreadState != System.Threading.ThreadState.Aborted && 
                ThreaD.ThreadState != System.Threading.ThreadState.AbortRequested)
            {
                SetEvent(EventoThread);
                ResetEvent(EventoThread);
            }
            else
            {
                ThreaD.Abort(); /*Envia sinal para parar a thread*/
                ThreaD.Join(); /*Espera a thread ser finalizada*/

                foreach (Control cLabel in Controls)
                {
                    if (cLabel.Name == "cContador")
                    {
                        ThreaD = null;
                        ThreaD = new Thread(() => TempoAtualiza(cLabel as Label));
                        ThreaD.Start();
                        break;
                    }
                }
            }
        }
    }
}