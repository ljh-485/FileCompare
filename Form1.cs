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
            try
            {
                // 폴더(디렉터리) 먼저 추가
                var dirs = Directory.EnumerateDirectories(folderPath)
                          .Select(p => new DirectoryInfo(p))
                          .OrderBy(d => d.Name);

                foreach (var d in dirs)
                {
                    var item = new ListViewItem(d.Name);
                    item.SubItems.Add("<DIR>");
                    item.SubItems.Add(d.LastWriteTime.ToString("g"));
                    item.ForeColor = Color.Black;
                    lv.Items.Add(item);
                }

                // 파일 추가
                var files = Directory.EnumerateFiles(folderPath)
                                        .Select(p => new FileInfo(p))
                                        .OrderBy(f => f.Name);

                foreach (var f in files)
                {
                    var item = new ListViewItem(f.Name);
                    item.SubItems.Add(f.Length.ToString("N0") + " 바이트");
                    item.SubItems.Add(f.LastWriteTime.ToString("g"));
                    item.ForeColor = Color.Black;
                    lv.Items.Add(item);
                }

                // 컬럼 너비 자동 조정(컨텐츠 기준)
                for (int i = 0; i < lv.Columns.Count; i++)
                {
                    lv.AutoResizeColumn(i, ColumnHeaderAutoResizeStyle.ColumnContent);
                }

                // 양쪽 폴더가 모두 선택되어 있으면 비교 수행
                if (!string.IsNullOrWhiteSpace(txtLeftDir.Text) &&
                    !string.IsNullOrWhiteSpace(txtRightDir.Text) &&
                    Directory.Exists(txtLeftDir.Text) &&
                    Directory.Exists(txtRightDir.Text))
                {
                    CompareAndColorize();
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

        private void CompareAndColorize()
        {
            // 왼쪽 ListView 색상 적용
            ColorizeListView(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);

            // 오른쪽 ListView 색상 적용
            ColorizeListView(lvwRightDir, txtRightDir.Text, txtLeftDir.Text);
        }

        private void ColorizeListView(ListView lv, string currentDir, string otherDir)
        {
            foreach (ListViewItem item in lv.Items)
            {
                string itemName = item.Text;
                string currentPath = Path.Combine(currentDir, itemName);
                string otherPath = Path.Combine(otherDir, itemName);
                bool isCurrentDir = item.SubItems[1].Text == "<DIR>";

                // 반대편에 같은 이름이 있는지 확인
                bool otherExists = isCurrentDir ? Directory.Exists(otherPath) : File.Exists(otherPath);

                if (!otherExists)
                {
                    // 한쪽에만 존재하는 단독 파일/폴더 - 보라색
                    item.ForeColor = Color.Purple;
                }
                else
                {
                    // 양쪽 다 존재 - 타입 확인
                    bool otherIsDir = Directory.Exists(otherPath);
                    bool otherIsFile = File.Exists(otherPath);

                    if (isCurrentDir && otherIsFile)
                    {
                        // 한쪽은 폴더, 한쪽은 파일 - 보라색
                        item.ForeColor = Color.Purple;
                    }
                    else if (!isCurrentDir && otherIsDir)
                    {
                        // 한쪽은 파일, 한쪽은 폴더 - 보라색
                        item.ForeColor = Color.Purple;
                    }
                    else if (!isCurrentDir && otherIsFile)
                    {
                        // 둘 다 파일 - 크기와 내용 비교
                        FileInfo currentFile = new FileInfo(currentPath);
                        FileInfo otherFile = new FileInfo(otherPath);

                        // 파일 크기가 다르면 내용이 다른 것
                        bool filesAreIdentical = false;

                        if (currentFile.Length == otherFile.Length)
                        {
                            // 크기가 같으면 내용 비교
                            filesAreIdentical = CompareFileContents(currentPath, otherPath);
                        }

                        if (filesAreIdentical)
                        {
                            // 내용이 완전히 동일 - 검은색
                            item.ForeColor = Color.Black;
                        }
                        else
                        {
                            // 내용이 다름 - 날짜 비교
                            if (currentFile.LastWriteTime < otherFile.LastWriteTime)
                            {
                                // 현재 파일이 더 오래됨 (날짜가 빠름, Old) - 빨간색
                                item.ForeColor = Color.Red;
                            }
                            else
                            {
                                // 현재 파일이 더 최신 (날짜가 느림, New) - 회색
                                item.ForeColor = Color.Gray;
                            }
                        }
                    }
                    else if (isCurrentDir && otherIsDir)
                    {
                        // 둘 다 폴더 - 검은색 (폴더는 내용 비교 안함)
                        item.ForeColor = Color.Black;
                    }
                }
            }
        }

        private void btnCopyFromLeft_Click(object sender, EventArgs e)
        {
            CopySelectedFiles(lvwLeftDir, txtLeftDir.Text, txtRightDir.Text);
        }

        private void btnCopyFromRight_Click(object sender, EventArgs e)
        {
            CopySelectedFiles(lvwRightDir, txtRightDir.Text, txtLeftDir.Text);
        }

        private void CopySelectedFiles(ListView sourceListView, string sourceDir, string targetDir)
        {
            if (sourceListView.SelectedItems.Count == 0)
            {
                MessageBox.Show(this, "복사할 파일 또는 폴더를 선택하세요.", "안내",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (string.IsNullOrWhiteSpace(targetDir) || !Directory.Exists(targetDir))
            {
                MessageBox.Show(this, "대상 폴더가 유효하지 않습니다.", "오류",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // 대상 ListView 찾기
            ListView targetListView = (sourceListView == lvwLeftDir) ? lvwRightDir : lvwLeftDir;

            foreach (ListViewItem item in sourceListView.SelectedItems)
            {
                string itemName = item.Text;
                string sourcePath = Path.Combine(sourceDir, itemName);
                string targetPath = Path.Combine(targetDir, itemName);

                // 회색(최신) 파일을 빨간색(오래됨) 파일로 복사하려는 경우 차단
                if (item.ForeColor == Color.Gray)
                {
                    // 대상 ListView에서 같은 이름의 파일 찾기
                    ListViewItem targetItem = FindItemByName(targetListView, itemName);
                    if (targetItem != null && targetItem.ForeColor == Color.Red)
                    {
                        MessageBox.Show(this, 
                            $"'{itemName}'는 최신 파일(회색)입니다.\n오래된 파일(빨간색)로 복사할 수 없습니다.", 
                            "복사 차단",
                            MessageBoxButtons.OK, 
                            MessageBoxIcon.Warning);
                        continue;
                    }
                }

                try
                {
                    bool isDirectory = item.SubItems[1].Text == "<DIR>";

                    if (isDirectory)
                    {
                        CopyDirectory(sourcePath, targetPath);
                    }
                    else
                    {
                        if (File.Exists(targetPath))
                        {
                            FileInfo sourceFile = new FileInfo(sourcePath);
                            FileInfo targetFile = new FileInfo(targetPath);

                            if (targetFile.LastWriteTime > sourceFile.LastWriteTime)
                            {
                                var result = MessageBox.Show(this,
                                    $"대상 파일이 원본 파일보다 최신입니다.\n\n" +
                                    $"원본: {sourceFile.LastWriteTime:g}\n" +
                                    $"대상: {targetFile.LastWriteTime:g}\n\n" +
                                    $"덮어쓰시겠습니까?",
                                    "확인",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Warning);

                                if (result != DialogResult.Yes)
                                {
                                    continue;
                                }
                            }
                        }

                        File.Copy(sourcePath, targetPath, true);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, $"'{itemName}' 복사 중 오류 발생:\n{ex.Message}", "오류",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }

            PopulateListView(lvwLeftDir, txtLeftDir.Text);
            PopulateListView(lvwRightDir, txtRightDir.Text);

            MessageBox.Show(this, "복사가 완료되었습니다.", "완료",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private ListViewItem FindItemByName(ListView lv, string name)
        {
            foreach (ListViewItem item in lv.Items)
            {
                if (item.Text == name)
                {
                    return item;
                }
            }
            return null;
        }

        private bool CompareFileContents(string file1Path, string file2Path)
        {
            try
            {
                const int bufferSize = 4096;
                byte[] buffer1 = new byte[bufferSize];
                byte[] buffer2 = new byte[bufferSize];

                using (FileStream fs1 = new FileStream(file1Path, FileMode.Open, FileAccess.Read))
                using (FileStream fs2 = new FileStream(file2Path, FileMode.Open, FileAccess.Read))
                {
                    int bytesRead1, bytesRead2;

                    while (true)
                    {
                        bytesRead1 = fs1.Read(buffer1, 0, bufferSize);
                        bytesRead2 = fs2.Read(buffer2, 0, bufferSize);

                        if (bytesRead1 != bytesRead2)
                            return false;

                        if (bytesRead1 == 0)
                            return true;

                        for (int i = 0; i < bytesRead1; i++)
                        {
                            if (buffer1[i] != buffer2[i])
                                return false;
                        }
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        private void CopyDirectory(string sourceDir, string targetDir)
        {
            if (!Directory.Exists(targetDir))
            {
                Directory.CreateDirectory(targetDir);
            }

            foreach (string filePath in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(filePath);
                string targetPath = Path.Combine(targetDir, fileName);
                File.Copy(filePath, targetPath, true);
            }

            foreach (string dirPath in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(dirPath);
                string targetPath = Path.Combine(targetDir, dirName);
                CopyDirectory(dirPath, targetPath);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }

}

