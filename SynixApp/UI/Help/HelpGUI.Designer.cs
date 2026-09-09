// ============================================================================
// PROJECT: Synix Game Server Control Panel
// AUTHOR: Jason Turner (ubidzz)
// COPYRIGHT: © 2026 All Rights Reserved.
//
// LEGAL NOTICE:
// This source code is proprietary and confidential.
// 1. Permission is granted for PERSONAL, NON-COMMERCIAL use only.
// 2. You may modify this code for your own use, but you may NOT redistribute,
//    rebrand, or sell this code or derivative works without written consent.
// 3. The "Synix" brand and logic remain the property of Jason Turner.
// ============================================================================

namespace Synix_Control_Panel.SynixApp.UI.Help
{
	partial class HelpGUI
	{
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing)
		{
			if (disposing)
			{
				components?.Dispose();
			}

			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpGUI));
			shellLayout = new TableLayoutPanel();
			titleBar = new Panel();
			picLogo = new PictureBox();
			lblWindowTitle = new Label();
			lblWindowSubtitle = new Label();
			btnMinimize = new Button();
			btnClose = new Button();
			titleBottomBorder = new Label();
			bodyLayout = new TableLayoutPanel();
			sidebarPanel = new Panel();
			lblSidebarEyebrow = new Label();
			lblSidebarTitle = new Label();
			lblSidebarDescription = new Label();
			searchCard = new Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard();
			lblSearchIcon = new Label();
			txtSearch = new TextBox();
			btnClearSearch = new Button();
			treeNavigation = new TreeView();
			sidebarFooterBorder = new Label();
			lblArticleCount = new Label();
			lblSearchHint = new Label();
			sidebarRightBorder = new Label();
			contentLayout = new TableLayoutPanel();
			contentHeader = new Panel();
			lblContentEyebrow = new Label();
			lblContentHeading = new Label();
			lblContentDescription = new Label();
			articleCard = new Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard();
			articleLayout = new TableLayoutPanel();
			articleHeader = new Panel();
			lblTopicCategory = new Label();
			lblTopicTitle = new Label();
			lblArticleBadge = new Label();
			articleDivider = new Label();
			articleBody = new Panel();
			lblAnswer = new RichTextBox();
			qrCard = new Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard();
			lblQrHeading = new Label();
			pbQRCode = new PictureBox();
			lblQrCaption = new Label();
			contentFooter = new Panel();
			lblFooterHint = new Label();
			lblFooterStatus = new Label();
			shellLayout.SuspendLayout();
			titleBar.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)picLogo).BeginInit();
			bodyLayout.SuspendLayout();
			sidebarPanel.SuspendLayout();
			searchCard.SuspendLayout();
			contentLayout.SuspendLayout();
			contentHeader.SuspendLayout();
			articleCard.SuspendLayout();
			articleLayout.SuspendLayout();
			articleHeader.SuspendLayout();
			articleBody.SuspendLayout();
			qrCard.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)pbQRCode).BeginInit();
			contentFooter.SuspendLayout();
			SuspendLayout();
			//
			// shellLayout
			//
			shellLayout.BackColor = Color.FromArgb(8, 13, 24);
			shellLayout.ColumnCount = 1;
			shellLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			shellLayout.Controls.Add(titleBar, 0, 0);
			shellLayout.Controls.Add(bodyLayout, 0, 1);
			shellLayout.Dock = DockStyle.Fill;
			shellLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
			shellLayout.Location = new Point(1, 1);
			shellLayout.Margin = new Padding(0);
			shellLayout.Name = "shellLayout";
			shellLayout.RowCount = 2;
			shellLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 56F));
			shellLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			shellLayout.Size = new Size(1178, 758);
			shellLayout.TabIndex = 0;
			//
			// titleBar
			//
			titleBar.BackColor = Color.FromArgb(6, 12, 22);
			titleBar.Controls.Add(picLogo);
			titleBar.Controls.Add(lblWindowTitle);
			titleBar.Controls.Add(lblWindowSubtitle);
			titleBar.Controls.Add(btnMinimize);
			titleBar.Controls.Add(btnClose);
			titleBar.Controls.Add(titleBottomBorder);
			titleBar.Dock = DockStyle.Fill;
			titleBar.Location = new Point(0, 0);
			titleBar.Margin = new Padding(0);
			titleBar.Name = "titleBar";
			titleBar.Size = new Size(1178, 56);
			titleBar.TabIndex = 0;
			titleBar.MouseDown += TitleBar_MouseDown;
			//
			// picLogo
			//
			picLogo.BackColor = Color.FromArgb(6, 12, 22);
			picLogo.Image = Properties.Resources.synix_logo;
			picLogo.Location = new Point(18, 13);
			picLogo.Name = "picLogo";
			picLogo.Size = new Size(30, 30);
			picLogo.SizeMode = PictureBoxSizeMode.Zoom;
			picLogo.TabIndex = 0;
			picLogo.TabStop = false;
			picLogo.MouseDown += TitleBar_MouseDown;
			//
			// lblWindowTitle
			//
			lblWindowTitle.AutoSize = true;
			lblWindowTitle.BackColor = Color.FromArgb(6, 12, 22);
			lblWindowTitle.Font = new Font("Segoe UI", 12F, FontStyle.Bold, GraphicsUnit.Point);
			lblWindowTitle.ForeColor = Color.FromArgb(245, 247, 251);
			lblWindowTitle.Location = new Point(58, 17);
			lblWindowTitle.Name = "lblWindowTitle";
			lblWindowTitle.Size = new Size(96, 21);
			lblWindowTitle.TabIndex = 1;
			lblWindowTitle.Text = "Help Center";
			lblWindowTitle.MouseDown += TitleBar_MouseDown;
			//
			// lblWindowSubtitle
			//
			lblWindowSubtitle.AutoSize = true;
			lblWindowSubtitle.BackColor = Color.FromArgb(6, 12, 22);
			lblWindowSubtitle.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
			lblWindowSubtitle.ForeColor = Color.FromArgb(105, 124, 153);
			lblWindowSubtitle.Location = new Point(169, 20);
			lblWindowSubtitle.Name = "lblWindowSubtitle";
			lblWindowSubtitle.Size = new Size(171, 15);
			lblWindowSubtitle.TabIndex = 2;
			lblWindowSubtitle.Text = string.Empty;
			lblWindowSubtitle.MouseDown += TitleBar_MouseDown;
			lblWindowSubtitle.Visible = false;
			//
			// btnMinimize
			//
			btnMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnMinimize.BackColor = Color.FromArgb(6, 12, 22);
			btnMinimize.Cursor = Cursors.Hand;
			btnMinimize.FlatAppearance.BorderSize = 0;
			btnMinimize.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 42, 60);
			btnMinimize.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 34, 52);
			btnMinimize.FlatStyle = FlatStyle.Flat;
			btnMinimize.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			btnMinimize.ForeColor = Color.FromArgb(245, 247, 251);
			btnMinimize.Location = new Point(1082, 0);
			btnMinimize.Name = "btnMinimize";
			btnMinimize.Size = new Size(48, 55);
			btnMinimize.TabIndex = 3;
			btnMinimize.TabStop = false;
			btnMinimize.Text = "—";
			btnMinimize.UseVisualStyleBackColor = false;
			btnMinimize.Click += btnMinimize_Click;
			//
			// btnClose
			//
			btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClose.BackColor = Color.FromArgb(6, 12, 22);
			btnClose.Cursor = Cursors.Hand;
			btnClose.FlatAppearance.BorderSize = 0;
			btnClose.FlatAppearance.MouseDownBackColor = Color.FromArgb(175, 35, 50);
			btnClose.FlatAppearance.MouseOverBackColor = Color.FromArgb(205, 48, 64);
			btnClose.FlatStyle = FlatStyle.Flat;
			btnClose.Font = new Font("Segoe UI", 15F, FontStyle.Regular, GraphicsUnit.Point);
			btnClose.ForeColor = Color.FromArgb(245, 247, 251);
			btnClose.Location = new Point(1130, 0);
			btnClose.Name = "btnClose";
			btnClose.Size = new Size(48, 55);
			btnClose.TabIndex = 4;
			btnClose.TabStop = false;
			btnClose.Text = "×";
			btnClose.UseVisualStyleBackColor = false;
			btnClose.Click += btnClose_Click;
			//
			// titleBottomBorder
			//
			titleBottomBorder.BackColor = Color.FromArgb(38, 52, 77);
			titleBottomBorder.Dock = DockStyle.Bottom;
			titleBottomBorder.Location = new Point(0, 55);
			titleBottomBorder.Margin = new Padding(0);
			titleBottomBorder.Name = "titleBottomBorder";
			titleBottomBorder.Size = new Size(1178, 1);
			titleBottomBorder.TabIndex = 5;
			//
			// bodyLayout
			//
			bodyLayout.BackColor = Color.FromArgb(8, 13, 24);
			bodyLayout.ColumnCount = 2;
			bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330F));
			bodyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			bodyLayout.Controls.Add(sidebarPanel, 0, 0);
			bodyLayout.Controls.Add(contentLayout, 1, 0);
			bodyLayout.Dock = DockStyle.Fill;
			bodyLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
			bodyLayout.Location = new Point(0, 56);
			bodyLayout.Margin = new Padding(0);
			bodyLayout.Name = "bodyLayout";
			bodyLayout.RowCount = 1;
			bodyLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			bodyLayout.Size = new Size(1178, 702);
			bodyLayout.TabIndex = 1;
			//
			// sidebarPanel
			//
			sidebarPanel.BackColor = Color.FromArgb(10, 18, 32);
			sidebarPanel.Controls.Add(lblSidebarEyebrow);
			sidebarPanel.Controls.Add(lblSidebarTitle);
			sidebarPanel.Controls.Add(lblSidebarDescription);
			sidebarPanel.Controls.Add(searchCard);
			sidebarPanel.Controls.Add(treeNavigation);
			sidebarPanel.Controls.Add(sidebarFooterBorder);
			sidebarPanel.Controls.Add(lblArticleCount);
			sidebarPanel.Controls.Add(lblSearchHint);
			sidebarPanel.Controls.Add(sidebarRightBorder);
			sidebarPanel.Dock = DockStyle.Fill;
			sidebarPanel.Location = new Point(0, 0);
			sidebarPanel.Margin = new Padding(0);
			sidebarPanel.Name = "sidebarPanel";
			sidebarPanel.Size = new Size(330, 702);
			sidebarPanel.TabIndex = 0;
			//
			// lblSidebarEyebrow
			//
			lblSidebarEyebrow.AutoSize = true;
			lblSidebarEyebrow.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
			lblSidebarEyebrow.ForeColor = Color.FromArgb(32, 214, 199);
			lblSidebarEyebrow.Location = new Point(20, 20);
			lblSidebarEyebrow.Name = "lblSidebarEyebrow";
			lblSidebarEyebrow.Size = new Size(103, 15);
			lblSidebarEyebrow.TabIndex = 0;
			lblSidebarEyebrow.Text = "KNOWLEDGE BASE";
			//
			// lblSidebarTitle
			//
			lblSidebarTitle.AutoSize = true;
			lblSidebarTitle.Font = new Font("Segoe UI", 18F, FontStyle.Bold, GraphicsUnit.Point);
			lblSidebarTitle.ForeColor = Color.FromArgb(245, 247, 251);
			lblSidebarTitle.Location = new Point(18, 40);
			lblSidebarTitle.Name = "lblSidebarTitle";
			lblSidebarTitle.Size = new Size(173, 32);
			lblSidebarTitle.TabIndex = 1;
			lblSidebarTitle.Text = "Browse topics";
			//
			// lblSidebarDescription
			//
			lblSidebarDescription.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			lblSidebarDescription.ForeColor = Color.FromArgb(158, 172, 194);
			lblSidebarDescription.Location = new Point(20, 76);
			lblSidebarDescription.Name = "lblSidebarDescription";
			lblSidebarDescription.Size = new Size(290, 34);
			lblSidebarDescription.TabIndex = 2;
			lblSidebarDescription.Text = "Search the full knowledge base or expand a category below.";
			//
			// searchCard
			//
			searchCard.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			searchCard.BackColor = Color.FromArgb(12, 21, 36);
			searchCard.BorderColor = Color.FromArgb(38, 52, 77);
			searchCard.Controls.Add(lblSearchIcon);
			searchCard.Controls.Add(txtSearch);
			searchCard.Controls.Add(btnClearSearch);
			searchCard.CornerRadius = 9;
			searchCard.FillColor = Color.FromArgb(12, 21, 36);
			searchCard.Location = new Point(18, 119);
			searchCard.Margin = new Padding(0);
			searchCard.Name = "searchCard";
			searchCard.Size = new Size(294, 46);
			searchCard.TabIndex = 3;
			//
			// lblSearchIcon
			//
			lblSearchIcon.Font = new Font("Segoe UI Symbol", 12F, FontStyle.Regular, GraphicsUnit.Point);
			lblSearchIcon.ForeColor = Color.FromArgb(105, 124, 153);
			lblSearchIcon.Location = new Point(12, 10);
			lblSearchIcon.Name = "lblSearchIcon";
			lblSearchIcon.Size = new Size(26, 25);
			lblSearchIcon.TabIndex = 0;
			lblSearchIcon.Text = "⌕";
			lblSearchIcon.TextAlign = ContentAlignment.MiddleCenter;
			//
			// txtSearch
			//
			txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			txtSearch.BackColor = Color.FromArgb(12, 21, 36);
			txtSearch.BorderStyle = BorderStyle.None;
			txtSearch.Font = new Font("Segoe UI", 10F, FontStyle.Regular, GraphicsUnit.Point);
			txtSearch.ForeColor = Color.FromArgb(245, 247, 251);
			txtSearch.Location = new Point(44, 13);
			txtSearch.Name = "txtSearch";
			txtSearch.Size = new Size(210, 18);
			txtSearch.TabIndex = 1;
			txtSearch.TextChanged += txtSearch_TextChanged;
			//
			// btnClearSearch
			//
			btnClearSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClearSearch.BackColor = Color.FromArgb(12, 21, 36);
			btnClearSearch.Cursor = Cursors.Hand;
			btnClearSearch.FlatAppearance.BorderSize = 0;
			btnClearSearch.FlatAppearance.MouseDownBackColor = Color.FromArgb(28, 75, 91);
			btnClearSearch.FlatAppearance.MouseOverBackColor = Color.FromArgb(20, 33, 54);
			btnClearSearch.FlatStyle = FlatStyle.Flat;
			btnClearSearch.Font = new Font("Segoe UI", 12F, FontStyle.Regular, GraphicsUnit.Point);
			btnClearSearch.ForeColor = Color.FromArgb(158, 172, 194);
			btnClearSearch.Location = new Point(258, 4);
			btnClearSearch.Name = "btnClearSearch";
			btnClearSearch.Size = new Size(32, 37);
			btnClearSearch.TabIndex = 2;
			btnClearSearch.TabStop = false;
			btnClearSearch.Text = "×";
			btnClearSearch.UseVisualStyleBackColor = false;
			btnClearSearch.Visible = false;
			btnClearSearch.Click += btnClearSearch_Click;
			//
			// treeNavigation
			//
			treeNavigation.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			treeNavigation.BackColor = Color.FromArgb(10, 18, 32);
			treeNavigation.BorderStyle = BorderStyle.None;
			treeNavigation.DrawMode = TreeViewDrawMode.OwnerDrawAll;
			treeNavigation.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
			treeNavigation.ForeColor = Color.FromArgb(158, 172, 194);
			treeNavigation.FullRowSelect = true;
			treeNavigation.HideSelection = false;
			treeNavigation.HotTracking = true;
			treeNavigation.Indent = 18;
			treeNavigation.ItemHeight = 38;
			treeNavigation.Location = new Point(18, 180);
			treeNavigation.Name = "treeNavigation";
			treeNavigation.ShowLines = false;
			treeNavigation.ShowNodeToolTips = true;
			treeNavigation.ShowPlusMinus = false;
			treeNavigation.ShowRootLines = false;
			treeNavigation.Size = new Size(294, 445);
			treeNavigation.TabIndex = 4;
			treeNavigation.AfterSelect += treeNavigation_AfterSelect;
			treeNavigation.DrawNode += treeNavigation_DrawNode;
			treeNavigation.NodeMouseClick += treeNavigation_NodeMouseClick;
			//
			// sidebarFooterBorder
			//
			sidebarFooterBorder.Anchor = AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			sidebarFooterBorder.BackColor = Color.FromArgb(38, 52, 77);
			sidebarFooterBorder.Location = new Point(18, 638);
			sidebarFooterBorder.Name = "sidebarFooterBorder";
			sidebarFooterBorder.Size = new Size(294, 1);
			sidebarFooterBorder.TabIndex = 5;
			//
			// lblArticleCount
			//
			lblArticleCount.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			lblArticleCount.AutoSize = true;
			lblArticleCount.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point);
			lblArticleCount.ForeColor = Color.FromArgb(245, 247, 251);
			lblArticleCount.Location = new Point(20, 651);
			lblArticleCount.Name = "lblArticleCount";
			lblArticleCount.Size = new Size(89, 15);
			lblArticleCount.TabIndex = 6;
			lblArticleCount.Text = "0 help articles";
			//
			// lblSearchHint
			//
			lblSearchHint.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
			lblSearchHint.AutoSize = true;
			lblSearchHint.Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point);
			lblSearchHint.ForeColor = Color.FromArgb(105, 124, 153);
			lblSearchHint.Location = new Point(20, 674);
			lblSearchHint.Name = "lblSearchHint";
			lblSearchHint.Size = new Size(164, 13);
			lblSearchHint.TabIndex = 7;
			lblSearchHint.Text = "Search checks titles and article text";
			//
			// sidebarRightBorder
			//
			sidebarRightBorder.BackColor = Color.FromArgb(38, 52, 77);
			sidebarRightBorder.Dock = DockStyle.Right;
			sidebarRightBorder.Location = new Point(329, 0);
			sidebarRightBorder.Margin = new Padding(0);
			sidebarRightBorder.Name = "sidebarRightBorder";
			sidebarRightBorder.Size = new Size(1, 702);
			sidebarRightBorder.TabIndex = 8;
			//
			// contentLayout
			//
			contentLayout.BackColor = Color.FromArgb(8, 13, 24);
			contentLayout.ColumnCount = 1;
			contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			contentLayout.Controls.Add(contentHeader, 0, 0);
			contentLayout.Controls.Add(articleCard, 0, 1);
			contentLayout.Controls.Add(contentFooter, 0, 2);
			contentLayout.Dock = DockStyle.Fill;
			contentLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
			contentLayout.Location = new Point(330, 0);
			contentLayout.Margin = new Padding(0);
			contentLayout.Name = "contentLayout";
			contentLayout.RowCount = 3;
			contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 116F));
			contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 58F));
			contentLayout.Size = new Size(848, 702);
			contentLayout.TabIndex = 1;
			//
			// contentHeader
			//
			contentHeader.BackColor = Color.FromArgb(8, 13, 24);
			contentHeader.Controls.Add(lblContentEyebrow);
			contentHeader.Controls.Add(lblContentHeading);
			contentHeader.Controls.Add(lblContentDescription);
			contentHeader.Dock = DockStyle.Fill;
			contentHeader.Location = new Point(0, 0);
			contentHeader.Margin = new Padding(0);
			contentHeader.Name = "contentHeader";
			contentHeader.Size = new Size(848, 116);
			contentHeader.TabIndex = 0;
			//
			// lblContentEyebrow
			//
			lblContentEyebrow.AutoSize = true;
			lblContentEyebrow.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
			lblContentEyebrow.ForeColor = Color.FromArgb(32, 214, 199);
			lblContentEyebrow.Location = new Point(32, 20);
			lblContentEyebrow.Name = "lblContentEyebrow";
			lblContentEyebrow.Size = new Size(94, 15);
			lblContentEyebrow.TabIndex = 0;
			lblContentEyebrow.Text = "HELP & SUPPORT";
			//
			// lblContentHeading
			//
			lblContentHeading.AutoSize = true;
			lblContentHeading.Font = new Font("Segoe UI", 24F, FontStyle.Bold, GraphicsUnit.Point);
			lblContentHeading.ForeColor = Color.FromArgb(245, 247, 251);
			lblContentHeading.Location = new Point(28, 36);
			lblContentHeading.Name = "lblContentHeading";
			lblContentHeading.Size = new Size(201, 45);
			lblContentHeading.TabIndex = 1;
			lblContentHeading.Text = "Help Center";
			//
			// lblContentDescription
			//
			lblContentDescription.AutoSize = true;
			lblContentDescription.Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point);
			lblContentDescription.ForeColor = Color.FromArgb(158, 172, 194);
			lblContentDescription.Location = new Point(33, 84);
			lblContentDescription.Name = "lblContentDescription";
			lblContentDescription.Size = new Size(413, 17);
			lblContentDescription.TabIndex = 2;
			lblContentDescription.Text = "Find setup guidance, command details, and troubleshooting answers.";
			//
			// articleCard
			//
			articleCard.BackColor = Color.FromArgb(17, 27, 45);
			articleCard.BorderColor = Color.FromArgb(38, 52, 77);
			articleCard.Controls.Add(articleLayout);
			articleCard.CornerRadius = 12;
			articleCard.Dock = DockStyle.Fill;
			articleCard.FillColor = Color.FromArgb(17, 27, 45);
			articleCard.Location = new Point(30, 116);
			articleCard.Margin = new Padding(30, 0, 28, 0);
			articleCard.Name = "articleCard";
			articleCard.Size = new Size(790, 528);
			articleCard.TabIndex = 1;
			//
			// articleLayout
			//
			articleLayout.BackColor = Color.FromArgb(17, 27, 45);
			articleLayout.ColumnCount = 1;
			articleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
			articleLayout.Controls.Add(articleHeader, 0, 0);
			articleLayout.Controls.Add(articleDivider, 0, 1);
			articleLayout.Controls.Add(articleBody, 0, 2);
			articleLayout.Dock = DockStyle.Fill;
			articleLayout.GrowStyle = TableLayoutPanelGrowStyle.FixedSize;
			articleLayout.Location = new Point(0, 0);
			articleLayout.Margin = new Padding(0);
			articleLayout.Name = "articleLayout";
			articleLayout.RowCount = 3;
			articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100F));
			articleLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 1F));
			articleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
			articleLayout.Size = new Size(790, 528);
			articleLayout.TabIndex = 0;
			//
			// articleHeader
			//
			articleHeader.BackColor = Color.FromArgb(17, 27, 45);
			articleHeader.Controls.Add(lblTopicCategory);
			articleHeader.Controls.Add(lblTopicTitle);
			articleHeader.Controls.Add(lblArticleBadge);
			articleHeader.Dock = DockStyle.Fill;
			articleHeader.Location = new Point(0, 0);
			articleHeader.Margin = new Padding(0);
			articleHeader.Name = "articleHeader";
			articleHeader.Size = new Size(790, 100);
			articleHeader.TabIndex = 0;
			//
			// lblTopicCategory
			//
			lblTopicCategory.AutoSize = true;
			lblTopicCategory.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
			lblTopicCategory.ForeColor = Color.FromArgb(32, 214, 199);
			lblTopicCategory.Location = new Point(24, 18);
			lblTopicCategory.Name = "lblTopicCategory";
			lblTopicCategory.Size = new Size(140, 15);
			lblTopicCategory.TabIndex = 0;
			lblTopicCategory.Text = "SYNIX KNOWLEDGE BASE";
			//
			// lblTopicTitle
			//
			lblTopicTitle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lblTopicTitle.AutoEllipsis = true;
			lblTopicTitle.Font = new Font("Segoe UI", 20F, FontStyle.Bold, GraphicsUnit.Point);
			lblTopicTitle.ForeColor = Color.FromArgb(245, 247, 251);
			lblTopicTitle.Location = new Point(20, 40);
			lblTopicTitle.Name = "lblTopicTitle";
			lblTopicTitle.Size = new Size(638, 40);
			lblTopicTitle.TabIndex = 1;
			lblTopicTitle.Text = "How can we help?";
			//
			// lblArticleBadge
			//
			lblArticleBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			lblArticleBadge.BackColor = Color.FromArgb(28, 75, 91);
			lblArticleBadge.Font = new Font("Segoe UI", 8F, FontStyle.Bold, GraphicsUnit.Point);
			lblArticleBadge.ForeColor = Color.FromArgb(32, 214, 199);
			lblArticleBadge.Location = new Point(678, 18);
			lblArticleBadge.Name = "lblArticleBadge";
			lblArticleBadge.Size = new Size(88, 26);
			lblArticleBadge.TabIndex = 2;
			lblArticleBadge.Text = "WELCOME";
			lblArticleBadge.TextAlign = ContentAlignment.MiddleCenter;
			//
			// articleDivider
			//
			articleDivider.BackColor = Color.FromArgb(38, 52, 77);
			articleDivider.Dock = DockStyle.Fill;
			articleDivider.Location = new Point(24, 100);
			articleDivider.Margin = new Padding(24, 0, 24, 0);
			articleDivider.Name = "articleDivider";
			articleDivider.Size = new Size(742, 1);
			articleDivider.TabIndex = 1;
			//
			// articleBody
			//
			articleBody.BackColor = Color.FromArgb(17, 27, 45);
			articleBody.Controls.Add(lblAnswer);
			articleBody.Controls.Add(qrCard);
			articleBody.Dock = DockStyle.Fill;
			articleBody.Location = new Point(0, 101);
			articleBody.Margin = new Padding(0);
			articleBody.Name = "articleBody";
			articleBody.Padding = new Padding(24, 20, 24, 20);
			articleBody.Size = new Size(790, 427);
			articleBody.TabIndex = 2;
			//
			// lblAnswer
			//
			lblAnswer.BackColor = Color.FromArgb(17, 27, 45);
			lblAnswer.BorderStyle = BorderStyle.None;
			lblAnswer.DetectUrls = true;
			lblAnswer.Dock = DockStyle.Fill;
			lblAnswer.Font = new Font("Segoe UI", 10.5F, FontStyle.Regular, GraphicsUnit.Point);
			lblAnswer.ForeColor = Color.FromArgb(214, 222, 234);
			lblAnswer.Location = new Point(24, 20);
			lblAnswer.Name = "lblAnswer";
			lblAnswer.ReadOnly = true;
			lblAnswer.ScrollBars = RichTextBoxScrollBars.Vertical;
			lblAnswer.Size = new Size(526, 387);
			lblAnswer.TabIndex = 0;
			lblAnswer.Text = "Welcome to the Synix Engine Knowledge Base. Select a topic from the navigation panel to begin.";
			lblAnswer.LinkClicked += lblAnswer_LinkClicked;
			//
			// qrCard
			//
			qrCard.BackColor = Color.FromArgb(12, 21, 36);
			qrCard.BorderColor = Color.FromArgb(38, 52, 77);
			qrCard.Controls.Add(lblQrHeading);
			qrCard.Controls.Add(pbQRCode);
			qrCard.Controls.Add(lblQrCaption);
			qrCard.CornerRadius = 10;
			qrCard.Dock = DockStyle.Right;
			qrCard.FillColor = Color.FromArgb(12, 21, 36);
			qrCard.Location = new Point(550, 20);
			qrCard.Margin = new Padding(0);
			qrCard.Name = "qrCard";
			qrCard.Size = new Size(216, 387);
			qrCard.TabIndex = 1;
			qrCard.Visible = false;
			//
			// lblQrHeading
			//
			lblQrHeading.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lblQrHeading.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
			lblQrHeading.ForeColor = Color.FromArgb(32, 214, 199);
			lblQrHeading.Location = new Point(18, 18);
			lblQrHeading.Name = "lblQrHeading";
			lblQrHeading.Size = new Size(180, 18);
			lblQrHeading.TabIndex = 0;
			lblQrHeading.Text = "SCAN TO SUPPORT SYNIX";
			lblQrHeading.TextAlign = ContentAlignment.MiddleCenter;
			//
			// pbQRCode
			//
			pbQRCode.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			pbQRCode.BackColor = Color.White;
			pbQRCode.Image = (Image)resources.GetObject("pbQRCode.Image");
			pbQRCode.Location = new Point(20, 50);
			pbQRCode.Name = "pbQRCode";
			pbQRCode.Padding = new Padding(8);
			pbQRCode.Size = new Size(176, 176);
			pbQRCode.SizeMode = PictureBoxSizeMode.Zoom;
			pbQRCode.TabIndex = 1;
			pbQRCode.TabStop = false;
			//
			// lblQrCaption
			//
			lblQrCaption.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			lblQrCaption.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
			lblQrCaption.ForeColor = Color.FromArgb(158, 172, 194);
			lblQrCaption.Location = new Point(18, 242);
			lblQrCaption.Name = "lblQrCaption";
			lblQrCaption.Size = new Size(180, 50);
			lblQrCaption.TabIndex = 2;
			lblQrCaption.Text = "Open the PayPal donation page on your phone.";
			lblQrCaption.TextAlign = ContentAlignment.TopCenter;
			//
			// contentFooter
			//
			contentFooter.BackColor = Color.FromArgb(8, 13, 24);
			contentFooter.Controls.Add(lblFooterHint);
			contentFooter.Controls.Add(lblFooterStatus);
			contentFooter.Dock = DockStyle.Fill;
			contentFooter.Location = new Point(0, 644);
			contentFooter.Margin = new Padding(0);
			contentFooter.Name = "contentFooter";
			contentFooter.Size = new Size(848, 58);
			contentFooter.TabIndex = 2;
			//
			// lblFooterHint
			//
			lblFooterHint.Anchor = AnchorStyles.Left;
			lblFooterHint.AutoSize = true;
			lblFooterHint.Font = new Font("Segoe UI", 8.5F, FontStyle.Regular, GraphicsUnit.Point);
			lblFooterHint.ForeColor = Color.FromArgb(105, 124, 153);
			lblFooterHint.Location = new Point(32, 22);
			lblFooterHint.Name = "lblFooterHint";
			lblFooterHint.Size = new Size(333, 15);
			lblFooterHint.TabIndex = 0;
			lblFooterHint.Text = "Ctrl+F  Search     •     Esc  Close     •     Offline help";
			//
			// lblFooterStatus
			//
			lblFooterStatus.Anchor = AnchorStyles.Right;
			lblFooterStatus.Font = new Font("Segoe UI", 8.5F, FontStyle.Bold, GraphicsUnit.Point);
			lblFooterStatus.ForeColor = Color.FromArgb(32, 214, 199);
			lblFooterStatus.Location = new Point(625, 20);
			lblFooterStatus.Name = "lblFooterStatus";
			lblFooterStatus.Size = new Size(195, 18);
			lblFooterStatus.TabIndex = 1;
			lblFooterStatus.Text = "KNOWLEDGE BASE READY";
			lblFooterStatus.TextAlign = ContentAlignment.MiddleRight;
			//
			// HelpGUI
			//
			AutoScaleDimensions = new SizeF(96F, 96F);
			AutoScaleMode = AutoScaleMode.Dpi;
			BackColor = Color.FromArgb(38, 52, 77);
			ClientSize = new Size(1180, 760);
			Controls.Add(shellLayout);
			DoubleBuffered = true;
			Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
			ForeColor = Color.FromArgb(245, 247, 251);
			FormBorderStyle = FormBorderStyle.None;
			Icon = (Icon)resources.GetObject("$this.Icon");
			KeyPreview = true;
			MinimumSize = new Size(980, 620);
			Name = "HelpGUI";
			Padding = new Padding(1);
			StartPosition = FormStartPosition.CenterParent;
			Text = "Synix Help Center";
			shellLayout.ResumeLayout(false);
			titleBar.ResumeLayout(false);
			titleBar.PerformLayout();
			((System.ComponentModel.ISupportInitialize)picLogo).EndInit();
			bodyLayout.ResumeLayout(false);
			sidebarPanel.ResumeLayout(false);
			sidebarPanel.PerformLayout();
			searchCard.ResumeLayout(false);
			searchCard.PerformLayout();
			contentLayout.ResumeLayout(false);
			contentHeader.ResumeLayout(false);
			contentHeader.PerformLayout();
			articleCard.ResumeLayout(false);
			articleLayout.ResumeLayout(false);
			articleHeader.ResumeLayout(false);
			articleHeader.PerformLayout();
			articleBody.ResumeLayout(false);
			qrCard.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)pbQRCode).EndInit();
			contentFooter.ResumeLayout(false);
			contentFooter.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private TableLayoutPanel shellLayout;
		private Panel titleBar;
		private PictureBox picLogo;
		private Label lblWindowTitle;
		private Label lblWindowSubtitle;
		private Button btnMinimize;
		private Button btnClose;
		private Label titleBottomBorder;
		private TableLayoutPanel bodyLayout;
		private Panel sidebarPanel;
		private Label lblSidebarEyebrow;
		private Label lblSidebarTitle;
		private Label lblSidebarDescription;
		private Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard searchCard;
		private Label lblSearchIcon;
		private TextBox txtSearch;
		private Button btnClearSearch;
		private TreeView treeNavigation;
		private Label sidebarFooterBorder;
		private Label lblArticleCount;
		private Label lblSearchHint;
		private Label sidebarRightBorder;
		private TableLayoutPanel contentLayout;
		private Panel contentHeader;
		private Label lblContentEyebrow;
		private Label lblContentHeading;
		private Label lblContentDescription;
		private Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard articleCard;
		private TableLayoutPanel articleLayout;
		private Panel articleHeader;
		private Label lblTopicCategory;
		private Label lblTopicTitle;
		private Label lblArticleBadge;
		private Label articleDivider;
		private Panel articleBody;
		private RichTextBox lblAnswer;
		private Synix_Control_Panel.SynixApp.Design.Controls.ModernSettingsCard qrCard;
		private Label lblQrHeading;
		private PictureBox pbQRCode;
		private Label lblQrCaption;
		private Panel contentFooter;
		private Label lblFooterHint;
		private Label lblFooterStatus;
	}
}
