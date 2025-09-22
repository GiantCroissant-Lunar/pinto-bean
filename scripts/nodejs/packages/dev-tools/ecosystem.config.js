module.exports = {
  apps: [
    {
      name: "adr-preview",
      script: "npm run adr:preview",
      cwd: process.cwd(),
      watch: ["../../../../docs/adr"],
      ignore_watch: ["node_modules", "*.log"],
      env: {
        PORT: 4004,
        NODE_ENV: "development"
      },
      error_file: "./logs/adr-preview-error.log",
      out_file: "./logs/adr-preview-out.log",
      log_file: "./logs/adr-preview.log"
    },
    {
      name: "docs-site",
      script: "npm start",
      cwd: "../docs-site",
      watch: ["docs", "src", "../../../../docs/adr"],
      ignore_watch: ["node_modules", "build", "*.log"],
      env: {
        PORT: 3000,
        NODE_ENV: "development"
      },
      error_file: "./logs/docs-site-error.log",
      out_file: "./logs/docs-site-out.log",
      log_file: "./logs/docs-site.log"
    },
    {
      name: "main-site",
      script: "npm run dev",
      cwd: "../../apps/main-site",
      watch: ["src", "pages", "app"],
      ignore_watch: ["node_modules", ".next", "*.log"],
      env: {
        PORT: 3001,
        NODE_ENV: "development"
      },
      error_file: "./logs/main-site-error.log",
      out_file: "./logs/main-site-out.log",
      log_file: "./logs/main-site.log"
    }
  ]
};
