const User = require('../models/User');
const jwt = require('jsonwebtoken');
const bcrypt = require('bcryptjs');
const { updateUserStats } = require('./updateUserStats');

exports.register = async (req, res) => {
  try {
    const { username, password } = req.body;

    // Verificar se o usuário já existe
    if (await User.findOne({ username })) {
      return res.status(400).json({ error: 'Nome já cadastrado!' });
    }

    const passwordHash = await bcrypt.hash(password, 10);
    const user = new User({ username, passwordHash });
    await user.save();

    // Gerar token JWT
    const token = jwt.sign({ userId: user._id }, process.env.JWT_SECRET, { expiresIn: '1h' });
    res.status(201).json({ token, userId: user._id, globalScore: user.globalScore, gamesPlayed: user.gamesPlayed, gamesWon: user.gamesWon });
  } catch (error) {
    console.error("Erro detalhado no registo:", error);
    res.status(500).json({ error: 'Erro no registro' });
  }
};

exports.login = async (req, res) => {
  try {
    const { username, password } = req.body;
    const user = await User.findOne({ username });

    if (!user || !(await bcrypt.compare(password, user.passwordHash))) {
      return res.status(401).json({ error: 'Credenciais inválidas!' });
    }

    const token = jwt.sign({ userId: user._id }, process.env.JWT_SECRET, { expiresIn: '1h' });
    res.json({ token, userId: user._id, globalScore: user.globalScore, gamesPlayed: user.gamesPlayed, gamesWon: user.gamesWon });
  } catch (error) {
    console.error(error);
    res.status(500).json({ error: error.message || 'Erro no login' });
  }
};

exports.updateStats = async (req, res) => {
  try {
    const userId = req.user._id;
    const { result, score } = req.body;

    console.log(`Atualizando stats para usuário ${userId}: result=${result}, score=${score}`);

    const globalScoreDelta = typeof score === 'number' ? score : 0;
    const gamesPlayedDelta = 1;
    const gamesWonDelta = result === 'win' ? 1 : 0;

    console.log(`Deltas calculados: globalScore=${globalScoreDelta}, gamesPlayed=${gamesPlayedDelta}, gamesWon=${gamesWonDelta}`);

    const updateResult = await updateUserStats(userId, {
      globalScoreDelta,
      gamesPlayedDelta,
      gamesWonDelta
    });

    if (updateResult.error) {
      console.error('Erro no updateUserStats:', updateResult.error);
      return res.status(400).json({ error: updateResult.error });
    }

    console.log('Stats atualizadas com sucesso:', updateResult.user);

    res.status(200).json({
      userId: updateResult.user.userId,
      globalScore: updateResult.user.globalScore,
      gamesPlayed: updateResult.user.gamesPlayed,
      gamesWon: updateResult.user.gamesWon
    });
  } catch (err) {
    console.error('Erro na rota updateStats:', err);
    res.status(500).json({ error: 'Erro ao atualizar estatísticas.' });
  }
};


