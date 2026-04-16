namespace FileCompare
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void splitContainer1_Panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void splitContainer1_Panel2_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnLeftDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";

                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) &&
                            Directory.Exists(txtLeftDir.Text))
                {
                    dlg.SelectedPath = txtLeftDir.Text;
                }

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtLeftDir.Text = dlg.SelectedPath;
                    PopulateListView(lvwLeftDir, dlg.SelectedPath);
                }
            }
        }

        private void btnRightDir_Click(object sender, EventArgs e)
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "폴더를 선택하세요.";

                // 현재 텍스트박스에 있는 경로를 초기 선택 폴더로 설정
                if (!string.IsNullOrWhiteSpace(txtRightDir.Text) &&
                            Directory.Exists(txtRightDir.Text))
                {
                    dlg.SelectedPath = txtRightDir.Text;
                }

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    txtRightDir.Text = dlg.SelectedPath;
                    PopulateListView(lvwRightDir, dlg.SelectedPath);
                }
            }
        }

        private void PopulateListView(ListView lv, string folderPath)
        {
            lv.BeginUpdate();
            lv.Items.Clear();
            try { // 폴더(디렉터리) 먼저 추가
                var dirs = Directory.EnumerateDirectories(folderPath)
                          .Select(p => new DirectoryInfo(p)) .OrderBy(d => d.Name);

                foreach (var d in dirs) {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));
                    lv.Items.Add(item);
                }


                // 파일 추가
                var files = Directory.EnumerateFiles(folderPath)
                                        .Select(p => new FileInfo(p))
                                        .OrderBy(f => f.Name);

                foreach (var f in files) // 여기서 f가 현재 리스트뷰에 추가할 파일(lf 역할)입니다.
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));

                    // 1. rf(반대편 파일) 정의하기
                    // 반대편 폴더 경로를 알아야 합니다. 
                    // 여기서는 예시로 txtLeftDir와 txtRightDir 중 현재 폴더가 아닌 곳을 찾습니다.
                    string targetDir = (folderPath == txtLeftDir.Text) ? txtRightDir.Text : txtLeftDir.Text;
                    string targetFilePath = Path.Combine(targetDir, f.Name);

                    FileInfo rf = null;
                    if (File.Exists(targetFilePath))
                    {
                        rf = new FileInfo(targetFilePath);
                    }

                    // 2. 상태 결정 및 색상 적용 (이제 rf를 사용할 수 있습니다)
                    if (rf != null)
                    {
                        // f.LastWriteTime(현재 파일)과 rf.LastWriteTime(반대편 파일) 비교
                        if (f.LastWriteTime == rf.LastWriteTime)
                        {
                            item.ForeColor = Color.Black;
                        }
                        else
                        {
                            item.ForeColor = Color.Red; // 시간이 다르면 빨간색 등으로 표시 가능
                        }
                    }
                    else
                    {
                        item.ForeColor = Color.Blue; // 반대편에 파일이 없는 경우
                    }

                    lv.Items.Add(item);
                }

                // 컬럼 너비 자동 조정(컨텐츠 기준)
                for (int i = 0; i < lv.Columns.Count; i++)
                { lv.AutoResizeColumn(i,
                    ColumnHeaderAutoResizeStyle.ColumnContent);
                }
            }
            catch (DirectoryNotFoundException)
            {
                MessageBox.Show(this, "폴더를 찾을 수 없습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (IOException ex)
            {
                MessageBox.Show(this, "입출력 오류: " + ex.Message, "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                lv.EndUpdate();
            }
        }

    }

}

